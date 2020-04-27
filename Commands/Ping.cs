using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("ping", "info", "Test", "Test", null)]
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task PingAsync()
        {

            var now = DateTime.Now;
            await Context.Channel.SendMessageAsync("Pinging...").Result.ModifyAsync(property =>
            {
                property.Content = $"Pong! Latency: {(DateTime.Now - now).TotalMilliseconds}ms, API Latency: {Context.Client.Latency}ms";
            });

        }
    }
}
