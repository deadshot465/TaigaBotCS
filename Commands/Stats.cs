using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            var embed = GetEmbeddedMessage(msg);

            var records = PersistenceService.GetUserRecord(commandName, Context.User.Id);
            if (records.First().Value is int || records.First().Value is double)
            {
                var orderedRecords = records.OrderByDescending(pair => (double)pair.Value);

                foreach (var record in orderedRecords)
                {
                    embed.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = $"**{record.Key}**",
                        Value = record.Value
                    });
                }
            }
            else
            {
                foreach (var record in records)
                {
                    var field = new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = $"**{record.Key}**",
                        Value = "- "
                    };

                    foreach (var ending in record.Value)
                    {
                        field.Value += $"__{ending.Key}__: {ending.Value}\n";
                    }

                    field.Value = field.Value.ToString().Replace("- ", string.Empty);

                    embed.Fields.Add(field);
                }
            }

            if (Helper.GetMemberConfig(Context.User.Id)?.Language == "jp")
                msg.Replace("time(s)", "回");

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("stats")]
        [Priority(7)]
        public async Task StatsAsync()
        {
            SetMemberConfig(Context.User.Id);

            var msg = _userLocalization["result"].ToString()
                .Replace("{user}", Context.User.Username)
                .Replace("{command}", string.Join(", ", _availableCommands));

            var embed = GetEmbeddedMessage(msg);

            var records = PersistenceService.GetUserRecord(Context.User.Id);
            var textInfo = new CultureInfo("en-us", false).TextInfo;

            foreach (var record in records)
            {
                embed.Fields.Add(new EmbedFieldBuilder
                {
                    IsInline = false,
                    Name = $"**{textInfo.ToTitleCase(record.Key)}**",
                    Value = _userLocalization["info"].ToString().Replace("{command}", record.Key)
                });

                if (record.Value.First().Value is int || record.Value.First().Value is double)
                {
                    var orderedRecords = record.Value.OrderByDescending(pair => (double)pair.Value);

                    foreach (var rec in orderedRecords)
                    {
                        embed.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = rec.Key,
                            Value = rec.Value
                        });
                    }
                }
                else
                {
                    foreach (var rec in record.Value)
                    {
                        var field = new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = rec.Key,
                            Value = "- "
                        };

                        foreach (var ending in rec.Value)
                        {
                            field.Value += $"__{ending.Key}__: {ending.Value}\n";
                        }

                        field.Value = field.Value.ToString().Replace("- ", string.Empty);

                        embed.Fields.Add(field);
                    }
                }
            }

            if (Helper.GetMemberConfig(Context.User.Id)?.Language == "jp")
                msg.Replace("time(s)", "回");

            await Context.Channel.SendMessageAsync(embed: embed.Build());
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

        private EmbedBuilder GetEmbeddedMessage(string baseMessage)
        {
            var user = Context.User as IGuildUser;

            return new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.User.GetAvatarUrl(),
                    Name = user.Nickname ?? user.Username
                },
                Description = baseMessage
            };
        }
    }
}
