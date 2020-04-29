using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Services
{
    public class AdminService
    {
        private readonly string _prefix;
        private readonly ulong _adminId;
        private readonly IServiceProvider _services;

        public AdminService(IServiceProvider services)
        {
            _services = services;
            _prefix = DotNetEnv.Env.GetString("ADMIN_PREFIX");
            _adminId = ulong.Parse(DotNetEnv.Env.GetString("ADMIN_ID"));
        }

        public async Task HandleAdminCommands(SocketUserMessage message)
        {
            var argPos = 0;
            if (!message.HasStringPrefix(_prefix, ref argPos)) return;
            if (message.Author.Id != _adminId) return;

            var parameters = message.Content.Substring(argPos).Split(' ');
            var cmd = parameters[0];
            var channel = message.Channel as SocketGuildChannel;

            switch (cmd)
            {
                case "setlang":
                    {
                        Console.WriteLine("Setting language code...");
                        var user = Helper.SearchUser(channel.Guild, parameters[1])[0];
                        if (user == null)
                        {
                            var msg = await message.Channel.SendMessageAsync("User not found.");
                            _ = DeleteMessageAfterTimeout(msg);
                        }
                        else
                        {
                            var memberConfig = Helper.GetMemberConfig(user.Id);
                            if (memberConfig == null)
                            {
                                Helper.AddMemberConfig(user, user.Id, parameters[2]);
                                var msg = await message.Channel
                                    .SendMessageAsync($"Successfully add the language for {user.Username}: {parameters[2]}");
                                _ = DeleteMessageAfterTimeout(msg);
                            }
                            else
                            {
                                memberConfig.Language = parameters[2];
                                var msg = await message.Channel
                                    .SendMessageAsync($"Successfully set the language for {user.Username}: {parameters[2]}");
                                _ = DeleteMessageAfterTimeout(msg);
                            }
                        }
                        _ = DeleteMessageAfterTimeout(message);
                        return;
                    }
                case "purge":
                    {
                        var amount = 100;
                        if (parameters.Length > 1)
                            amount = int.Parse(parameters[1]);
                        var collection = await message.Channel.GetMessagesAsync(amount).FlattenAsync();
                        var warning = await message.Channel
                            .SendMessageAsync($"The last {amount} messages in this channel will be deleted in 5 seconds.");
                        _ = DeleteMessageAfterTimeout(warning);
                        await Task.Delay(5 * 1000);
                        await (message.Channel as ITextChannel).DeleteMessagesAsync(collection);
                        return;
                    }
                default:
                    {
                        var msg = await message.Channel
                            .SendMessageAsync("Invalid admin command.");
                        _ = DeleteMessageAfterTimeout(msg);
                        return;
                    }
            }
        }

        private async Task DeleteMessageAfterTimeout(IUserMessage message, int timeout = 5)
        {
            await Task.Delay(timeout * 1000);
            await message.DeleteAsync();
        }
    }
}
