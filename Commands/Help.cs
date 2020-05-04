// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

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
            => await HelpAsync("help");

        [Command("help")]
        public async Task HelpAsync(string commandName)
        {
            SetMemberConfig(Context.User.Id);
            var helpErrors = _helpCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var allCommands = GetAllCommands();

            string[] categories = new[] { "fun", "info", "list", "util" };

            if (categories.Contains(commandName))
            {
                await Context.Channel.SendMessageAsync(ShowCommandLists(commandName));
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

        private string ShowCommandLists(string category)
        {
            var helpErrors = _helpCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var allCommands = GetAllCommands();
            IEnumerable<string> commandTexts;

            if (category == "list")
            {
                commandTexts = allCommands
                    .Select(t =>
                    {
                        var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                        return $"- **{attr.Category}:** `{attr.Name}`: *{attr.Description}*";
                    });
            }
            else
            {
                commandTexts = allCommands
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
            }

            var commandListText = string.Join("\n", commandTexts);

            var msg = category == "list" ?
                helpErrors["show_list"].ToString() :
                helpErrors["show_category"].ToString()
                .Replace("{category}", category);

            return msg.Replace("{commandLists}", commandListText);
        }
    }
}