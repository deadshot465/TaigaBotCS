using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS
{
    class Program
    {
        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();
            Helper.ReadMemberConfigs();
            Helper.LoadLocalizedStrings();
            new Program().MainAsync().GetAwaiter().GetResult();

            Console.WriteLine("Saving configurations");
            Helper.WriteMemberConfigs();
        }

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                
                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                await client.LoginAsync(TokenType.Bot, DotNetEnv.Env.GetString("TOKEN"));
                await client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                await Task.Delay(-1);
            }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<DialogService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
