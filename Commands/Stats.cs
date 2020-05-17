using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("stats", "info", null, null)]
    public class Stats : ModuleBase<SocketCommandContext>, IErrorHandler, IMemberConfigurable
    {
        private enum StatError
        {
            NoSuchCommand
        }

        private readonly string[] _availableCommands = new[]
        {
            "route", "valentine"
        };

        private Dictionary<string, object> _userLocalization;

        public void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _userLocalization = responseText.texts.stats;
        }

        [Command("stats")]
        [Priority(10)]
        public async Task StatsAsync(string commandName)
        {
            SetMemberConfig(Context.User.Id);

            if (!_availableCommands.Contains(commandName))
            {
                await HandleErrorAsync(StatError.NoSuchCommand);
                return;
            }

            var msg = _userLocalization["result"].ToString()
                .Replace("{user}", Context.User.Username)
                .Replace("{command}", commandName);

            var records = PersistenceService.GetUserRecord(commandName, Context.User.Id);

            foreach (var record in records)
            {
                if (record.Value is int)
                {
                    msg += $"**{record.Key}**: {record.Value} time(s)\n";
                }
                else
                {
                    msg += $"**{record.Key}**: \n";

                    foreach (var ending in record.Value)
                    {
                        msg += $"    - __{ending.Key}__: {ending.Value} time(s)\n";
                    }
                }
            }

            if (Helper.GetMemberConfig(Context.User.Id)?.Language == "jp")
                msg.Replace("time(s)", "回");

            await Context.Channel.SendMessageAsync(msg);
        }

        [Command("stats")]
        [Priority(7)]
        public async Task StatsAsync()
        {
            SetMemberConfig(Context.User.Id);

            var msg = _userLocalization["result"].ToString()
                .Replace("{user}", Context.User.Username)
                .Replace("{command}", string.Join(", ", _availableCommands));

            var records = PersistenceService.GetUserRecord(Context.User.Id);

            foreach (var record in records)
            {
                msg += $"`{record.Key}` - \n";

                foreach (var rec in record.Value)
                {
                    if (rec.Value is int)
                    {
                        msg += $"**{rec.Key}**: {rec.Value} time(s)\n";
                    }
                    else
                    {
                        msg += $"**{rec.Key}**: \n";

                        foreach (var ending in rec.Value)
                        {
                            msg += $"    - __{ending.Key}__: {ending.Value} time(s)\n";
                        }
                    }
                }
            }

            if (Helper.GetMemberConfig(Context.User.Id)?.Language == "jp")
                msg.Replace("time(s)", "回");

            await Context.Channel.SendMessageAsync(msg);
        }

        [Command("stats")]
        [Priority(5)]
        public async Task StatsAsync(params string[] text)
            => await StatsAsync(text[0]);

        public async Task HandleErrorAsync(Enum error)
        {
            SetMemberConfig(Context.User.Id);
            
            var err = (StatError)error;

            var msg = err switch
            {
                StatError.NoSuchCommand => (_userLocalization["errors"] as Dictionary<string, object>)["no_such_command"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
