// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("help", "info", null, null)]
    public class Help : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        private Dictionary<ulong, Dictionary<string, object>> _helpCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        [Command("help")]
        public async Task HelpAsync()
            => await HelpAsync("list");

        [Command("help")]
        public async Task HelpAsync(string commandName)
        {
            SetMemberConfig(Context.User.Id);
            var helpErrors = _helpCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var allCommands = GetAllCommands();

            string[] categories = new[] { "fun", "info", "list", "util" };

            if (categories.Contains(commandName))
            {
                var result = ShowCommandLists(commandName);
                if (result is Embed embed)
                    await Context.Channel.SendMessageAsync(embed: embed);
                else
                    await Context.Channel.SendMessageAsync(result);
                return;
            }

            string[] matureHiroCommandNames = new[]
            {
                "mhiro", "mhirosay", "maturehiro", "maturehirosay"
            };

            if (matureHiroCommandNames.Contains(commandName.Trim().ToLower()))
            {
                await Context.Channel.SendMessageAsync(helpErrors["no_command"].ToString());
                return;
            }

            var commandExist = allCommands
                .Where(t =>
                {
                    var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                    return attr.Name == commandName ||
                    (attr.Aliases != null && attr.Aliases.Contains(commandName));

                })
                .Count() > 0;

            if (!commandExist)
            {
                await Context.Channel.SendMessageAsync(helpErrors["no_command"].ToString());
                return;
            }

            Attributes.CommandAttribute commandAttribute = null;

            foreach (var t in allCommands)
            {
                var _type = t.UnderlyingSystemType;
                var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();

                if (attr.Name == commandName ||
                    (attr.Aliases != null && attr.Aliases.Contains(commandName)))
                {
                    commandAttribute = TypeDescriptor
                        .GetAttributes(_type)
                        .OfType<Attributes.CommandAttribute>()
                        .First();
                    break;
                }
            }

            var attrs = TypeDescriptor.GetAttributes(typeof(Convert))
                .OfType<Attributes.CommandAttribute>();

            var text = _helpCommandTexts[Context.User.Id]["result"].ToString()
                .Replace("{category}", commandAttribute.Category)
                .Replace("{name}", commandAttribute.Name)
                .Replace("{usage}", commandAttribute.Usage)
                .Replace("{aliases}",
                (commandAttribute.Aliases != null && commandAttribute.Aliases.Length > 0) ?
                $"\n**Aliases:** `{string.Join(", ", commandAttribute.Aliases)}`" :
                "");

            await Context.Channel.SendMessageAsync(text);
        }

        [Command("help")]
        public async Task HelpAsync(string commandName, params string[] rest)
            => await HelpAsync(commandName);

        public void SetMemberConfig(ulong userId)
        {
            if (_helpCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper.GetLocalization(
                Helper.GetMemberConfig(userId)?.Language);
            _helpCommandTexts[Context.User.Id] = responseText.texts.help;
        }

        private IEnumerable<Type> GetAllCommands()
        {
            var allCommands = typeof(Help).Assembly
                .GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(Attributes.CommandAttribute), true).Length > 0);

            foreach (var cmd in allCommands)
                yield return cmd;
        }

        private dynamic ShowCommandLists(string category)
        {
            var helpErrors = _helpCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;
            var helpInfos = _helpCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;

            if (category == "list")
            {
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Png),
                        Name = Context.User.Username
                    },
                    Color = new Color(0xe81615),
                    Description = helpInfos["show_list"].ToString()
                };

                var allCommands = GetAllCommands()
                    .Select(t =>
                    {
                        var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                        return (attr.Category, attr.Name);
                    })
                    .OrderBy(element => element.Name)
                    .GroupBy(element => element.Category, element => element.Name,
                    (_category, _names) =>
                    {
                        var _categories = helpInfos[_category] as Dictionary<string, object>;

                        return new
                        {
                            Category = _categories["type"].ToString(),
                            Commands = string.Join(' ', _names
                            .Where(name => name != "mhirosay")
                            .Select(name => '`' + name + '`')),
                            Icon = _categories["icon"].ToString()
                        };
                    })
                    .OrderBy(element => element.Category);

                var inline = true;
                for (var i = 0; i < allCommands.Count(); i++)
                {
                    embed.AddField(new EmbedFieldBuilder
                    {
                        IsInline = inline,
                        Name = allCommands.ElementAt(i).Icon + allCommands.ElementAt(i).Category,
                        Value = allCommands.ElementAt(i).Commands
                    });

                    inline = !(i != 0 && i % 3 == 0);
                }

                return embed.Build();
            }
            else
            {
                var allCommands = GetAllCommands();
                var commandTexts = allCommands
                    .Where(t =>
                    {
                        var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                        return attr.Category == category;
                    })
                    .Select(t =>
                    {
                        var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                        return $"- `{attr.Name}`: *{attr.Description}*";
                    });

                var commandListText = string.Join("\n", commandTexts);
                var msg = helpInfos["show_category"].ToString()
                    .Replace("{category}", category);

                return msg.Replace("{commandLists}", commandListText);
            }    
        }
    }
}