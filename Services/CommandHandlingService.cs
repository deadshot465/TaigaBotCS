using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Services
{
    class CommandHandlingService
    {
        private readonly string PREFIX = DotNetEnv.Env.GetString("PREFIX");

        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private Dictionary<string, Dictionary<ulong, DateTime>> _cooldowns =
            new Dictionary<string, Dictionary<ulong, DateTime>>();
        private Random _rng = new Random();
        private Stopwatch _stopWatch = new Stopwatch();

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            var presences = Helper.GetLocalization("en").texts.presence;
            var activity = presences[_rng.Next(0, presences.Length)];
            _client.SetActivityAsync(new Game(activity, ActivityType.Playing));

            _commands.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.MessageReceived += SetPresence;
            _client.MessageReceived += WriteMemberConfigs;
            _stopWatch.Start();
        }

        public async Task ExecuteWithDelay(Action action, double timeOut)
        {
            await Task.Delay((int)timeOut);
            action.Invoke();
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var authorId = message.Author.Id;
            var memberConfig = Helper.GetMemberConfig(authorId);
            var responseText = (memberConfig?.Language == "en") ? Helper.GetLocalization("en")
                : Helper.GetLocalization("jp");

            // Don't do anything in venting channel
            var ignoreChannels = new[] { DotNetEnv.Env.GetString("VENTCHN") };
            if (ignoreChannels.Contains(message.Channel.Id.ToString())) return;

            var argPos = 0;
            var botChannelId = DotNetEnv.Env.GetString("BOTCHN");
            var botModChannelId = DotNetEnv.Env.GetString("BOTMODCHN");
            var testChannelId = DotNetEnv.Env.GetString("TESTCHN");
            var messageChannelId = message.Channel.Id.ToString();

            // If the command doesn't start with the prefix, ignore it
            if (!message.HasStringPrefix(PREFIX, ref argPos)) return;

            // Limit command usages to specific channels
            if (((botChannelId.Length > 0 &&botModChannelId.Length > 0) &&
                (messageChannelId != botChannelId &&
                messageChannelId != botModChannelId)) &&
                (testChannelId.Length > 0 && messageChannelId != testChannelId))
            {
                return;
            }

            var command = message.Content.Substring(argPos).Split(' ')[0];

            // Check if the user is on cooldown
            var now = DateTime.Now;
            var res = _cooldowns.TryGetValue(command, out var timestamps);
            var attribute = typeof(CommandHandlingService).Assembly
                .GetCustomAttributes()
                .Where(attr =>
                {
                    return attr.GetType() == typeof(Commands.Attributes.CommandAttribute) &&
                    ((Commands.Attributes.CommandAttribute)attr).Name == command;
                })
                .First() as Commands.Attributes.CommandAttribute;
            var cooldownAmount = attribute.Cooldown * 1000.0;

            if (timestamps.TryGetValue(message.Author.Id, out var startTime))
            {
                var expirationTime = startTime.AddMilliseconds(cooldownAmount);

                if (now < expirationTime)
                {
                    var timeLeft = (expirationTime - now).TotalMilliseconds / 1000.0;
                    var cooldownMsg = responseText.texts.cooldown
                        .Replace("{timeLeft}", (Math.Round(timeLeft * 10.0) / 10.0).ToString())
                        .Replace("{cmd}", command);
                    await message.Channel.SendMessageAsync(cooldownMsg);
                    return;
                }
            }

            if (!memberConfig.HasValue)
            {
                Helper.AddMemberConfig(message.Author, message.Author.Id, "en");
                Console.WriteLine("Member config pushed to the array.");
            }
            else
            {
                Console.WriteLine(memberConfig);
                Console.WriteLine("Found config.");
            }
            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo,
            ICommandContext context, IResult result)
        {
            var authorId = context.Message.Author.Id;
            var memberConfig = Helper.GetMemberConfig(authorId);
            var responseText = (memberConfig?.Language == "en") ? Helper.GetLocalization("en")
                : Helper.GetLocalization("jp");

            // If command not found, fail the command and return
            if (!commandInfo.IsSpecified)
            {
                var cmd = context.Message.Content.Substring(PREFIX.Length)
                    .Split(' ')[0];

                var failedMessages = responseText.texts.failed_messages;
                var msg = failedMessages[_rng.Next(0, failedMessages.Length)]
                    .Replace("{command}", cmd);
                await context.Channel.SendMessageAsync(msg);
                return;
            }

            var command = commandInfo.Value.Name;

            // Add the command to the cooldown
            if (!_cooldowns.ContainsKey(command))
                _cooldowns[command] = new Dictionary<ulong, DateTime>();

            // Set the cooldown's timestamp
            var now = DateTime.Now;
            var res = _cooldowns.TryGetValue(command, out var timestamps);
            var attribute = commandInfo.Value.Module.Attributes
                .Where(attr => attr.GetType() == typeof(Commands.Attributes.CommandAttribute))
                .First() as Commands.Attributes.CommandAttribute;
            var cooldownAmount = attribute.Cooldown * 1000.0;

            // Add the author to the cooldown timestamps,
            // then remove the command after cooldown expires.
            timestamps.Add(authorId, DateTime.Now);
            ExecuteWithDelay(() => timestamps.Remove(authorId), cooldownAmount);

            if (result.IsSuccess) return;

            Console.Error.WriteLine($"Error: {result}");
            await context.Channel.SendMessageAsync(responseText.texts.execution_failure);
        }

        public async Task SetPresence(SocketMessage rawMessage)
        {
            if (_stopWatch.Elapsed.TotalMinutes >= 60)
            {
                Console.WriteLine("Setting presence");

                var presences = Helper.GetLocalization("en").texts.presence;
                var activity = presences[_rng.Next(0, presences.Length)];

                await _client.SetActivityAsync(new Game(activity, ActivityType.Playing));
            }

            await Task.CompletedTask;
        }

        public async Task WriteMemberConfigs(SocketMessage rawMessage)
        {
            if (_stopWatch.Elapsed.TotalMinutes >= 60)
            {
                Console.WriteLine("Writing member configs");
                _stopWatch.Restart();
                await Helper.WriteMemberConfigs();
            }

            await Task.CompletedTask;
        }
    }
}
