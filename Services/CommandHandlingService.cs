// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

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
        private readonly TaigaService _taigaService;
        private readonly AdminService _adminService;
        private readonly ReminderService _reminderService;
        private readonly IServiceProvider _services;

        private Dictionary<string, Dictionary<ulong, DateTime>> _cooldowns =
            new Dictionary<string, Dictionary<ulong, DateTime>>();
        private Random _rng = new Random();
        private Stopwatch _stopWatch = new Stopwatch();

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _taigaService = services.GetRequiredService<TaigaService>();
            _adminService = services.GetRequiredService<AdminService>();
            _reminderService = services.GetRequiredService<ReminderService>();
            _services = services;

            // Setting initial presence
            var presences = Helper.GetLocalization("en").texts.presence;
            var activity = presences[_rng.Next(0, presences.Length)];
            _client.SetActivityAsync(new Game(activity, ActivityType.Playing));

            _commands.CommandExecuted += CommandExecutedAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.MessageReceived += SetPresenceAsync;
            _client.MessageReceived += WriteMemberConfigsAsync;
            _client.UserJoined += UserJoinedAsync;
            _client.LatencyUpdated += LatencyUpdateAsync;
            _stopWatch.Start();
        }

        public async Task InitializeAsync()
        {
            await PersistenceService.LoadDialogData();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // Don't do anything in venting channel
            var ignoreChannels = new[] { DotNetEnv.Env.GetString("VENTCHN") };
            if (ignoreChannels.Contains(message.Channel.Id.ToString())) return;

            // Invoke Taiga service for reactions
            _ = _taigaService.HandleMessageAsync(message);

            // Admin commands
            _ = _adminService.HandleAdminCommands(message);

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

            var authorId = message.Author.Id;
            var memberConfig = Helper.GetMemberConfig(authorId);
            var responseText = Helper.GetLocalization(memberConfig?.Language);

            // Check if the user is on cooldown
            var allCommands = typeof(CommandHandlingService).Assembly
                .GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(Commands.Attributes.CommandAttribute), true).Length > 0);
            var attribute = allCommands
                .Where(t =>
                {
                    var attr = t.GetCustomAttribute<Commands.Attributes.CommandAttribute>();
                    return attr.Name == command ||
                    (attr.Aliases != null && attr.Aliases.Contains(command));
                })
                .First().GetCustomAttribute<Commands.Attributes.CommandAttribute>();
            var cooldownAmount = attribute.Cooldown * 1000.0;
            var now = DateTime.Now;
            var res = _cooldowns.TryGetValue(command, out var timestamps);
            if (!res && attribute.Aliases != null && attribute.Aliases.Length > 0)
            {
                res = _cooldowns.TryGetValue(attribute.Name, out timestamps);
            }

            if (timestamps != null && timestamps.TryGetValue(message.Author.Id, out var startTime))
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

            // Setting up member configurations
            if (memberConfig == null)
                Helper.AddMemberConfig(message.Author, message.Author.Id, "en");
            
            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> commandInfo,
            ICommandContext context, IResult result)
        {
            var authorId = context.Message.Author.Id;
            var memberConfig = Helper.GetMemberConfig(authorId);
            var responseText = Helper.GetLocalization(memberConfig?.Language);

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
            // then remove the command after cooldown expires
            timestamps.Add(authorId, DateTime.Now);
            _ = Task.Delay((int)cooldownAmount).ContinueWith(_ => timestamps.Remove(authorId));

            if (result.IsSuccess) return;

            Console.Error.WriteLine($"Error: {result}");
            await context.Channel.SendMessageAsync(responseText.texts.execution_failure);
        }

        public async Task SetPresenceAsync(SocketMessage rawMessage)
        {
            if (_stopWatch.Elapsed.TotalMinutes >= 60)
            {
                Console.WriteLine("Setting presence");

                var presences = Helper.GetLocalization("en").texts.presence;
                var activity = presences[_rng.Next(0, presences.Length)];

                await _client.SetActivityAsync(new Game(activity, ActivityType.Playing));
            }
        }

        public async Task UserJoinedAsync(SocketGuildUser user)
        {
            var generalChannelIds = new[]
            {
                DotNetEnv.Env.GetString("GENCHN"),
                DotNetEnv.Env.GetString("TESTGENCHN"),
            };

            foreach (var channelId in generalChannelIds)
            {
                var channel = user.Guild.GetTextChannel(ulong.Parse(channelId));
                if (channel == null) continue;

                var greetings = Helper.GetLocalization("en").texts.greetings;
                var msg = greetings[_rng.Next(0, greetings.Length)]
                    .Replace("{name}", $"<@{user.Id}>");

                await channel.SendMessageAsync(msg);
            }
        }

        public async Task WriteMemberConfigsAsync(SocketMessage rawMessage)
        {
            if (_stopWatch.Elapsed.TotalMinutes >= 60)
            {
                Console.WriteLine("Writing member configs");
                _stopWatch.Restart();
                await Helper.WriteMemberConfigs();
            }
        }

        public async Task LatencyUpdateAsync(int old, int value)
        {
            foreach (var reminder in _reminderService.RemindUser())
            {
                var channel = _client
                    .GetChannel(ulong.Parse(DotNetEnv.Env.GetString("BOTCHN"))) as ITextChannel;
                var user = (channel != null) ? await channel.GetUserAsync(reminder.Item1) : null;

                if (channel == null || user == null)
                {
                    channel = _client
                    .GetChannel(ulong.Parse(DotNetEnv.Env.GetString("TESTCHN"))) as ITextChannel;
                    user = (channel != null) ? await channel.GetUserAsync(reminder.Item1) : null;
                }
                
                if (channel != null && user != null)
                {
                    await channel.SendMessageAsync($"<@{reminder.Item1}> {reminder.Item2}");
                    await user.SendMessageAsync(reminder.Item2);
                }
            }

            await PersistenceService.WriteUserRecord();
        }
    }
}
