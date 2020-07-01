//    Taiga Bot. A bot that aims to provide interactive experiences to Taiga's fans.
//    Copyright(C) 2020 Tetsuki Syu
//    
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//    
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//    
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see < https://www.gnu.org/licenses/>.
//
//    **This is a modified and rewritten version of Yuuto bot.**
//    **Please refer to Yuuto bot's repository for more information.**
//    **https://github.com/Yuuto-Project/yuuto-bot**
//    **Codebase also influenced by Hiro bot by dunste123.**
//    **https://github.com/dunste123/hirobot**

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
            var config = new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug, CaseSensitiveCommands = false, DefaultRunMode = RunMode.Async
            };
            
            var commandService = new CommandService(config);

            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(commandService)
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<DialogService>()
                .AddSingleton<ImageService>()
                .AddSingleton<ShipService>()
                .AddSingleton<TaigaService>()
                .AddSingleton<AdminService>()
                .AddSingleton<MealService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<ReminderService>()
                .AddSingleton<AuthenticationService>()
                .BuildServiceProvider();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
