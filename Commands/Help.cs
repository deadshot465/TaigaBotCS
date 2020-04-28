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

            if (commandName == "list")
            {
                var commandTexts = GetAllCommands()
                .Select(t =>
                {
                    var attr = t.GetCustomAttribute<Attributes.CommandAttribute>();
                    return $"- **{attr.Category}:** `{attr.Name}`: *{attr.Description}*";
                });
                var commandListText = string.Join("\n", commandTexts);

                var msg = helpErrors["show_list"].ToString()
                    .Replace("{commandLists}", commandListText);

                await Context.Channel.SendMessageAsync(msg);
                return;
            }

            var allCommands = GetAllCommands();
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

            foreach (var t in GetAllCommands())
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
    }
}