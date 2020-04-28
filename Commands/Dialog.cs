// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("dialog", "fun", "dialog", null)]
    public class Dialog : ModuleBase<SocketCommandContext>
    {
        public DialogService DialogService { get; set; }
        
        private static readonly string[] _backgrounds = new[]
        {
            "bath", "beach", "cabin", "camp",
            "cave", "forest", "messhall"
        };

        private static readonly string[] _characters = new[]
        {
            "aiden", "avan", "chiaki", "connor", "eduard", "felix", "goro", "hiro",
            "hunter", "jirou", "keitaro", "kieran", "knox", "lee", "naoto", "natsumi",
            "seto", "taiga", "yoichi", "yoshi", "yuri", "yuuto"
        };

        private static readonly string _backgroundString = string.Join(", ", _backgrounds);
        private static readonly string _characterString = string.Join(", ", _characters);
        private static readonly Regex _emojiRegex
            = new Regex(@"(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff]|[\u0023-\u0039]\ufe0f?\u20e3|\u3299|\u3297|\u303d|\u3030|\u24c2|\ud83c[\udd70-\udd71]|\ud83c[\udd7e-\udd7f]|\ud83c\udd8e|\ud83c[\udd91-\udd9a]|\ud83c[\udde6-\uddff]|\ud83c[\ude01-\ude02]|\ud83c\ude1a|\ud83c\ude2f|\ud83c[\ude32-\ude3a]|\ud83c[\ude50-\ude51]|\u203c|\u2049|[\u25aa-\u25ab]|\u25b6|\u25c0|[\u25fb-\u25fe]|\u00a9|\u00ae|\u2122|\u2139|\ud83c\udc04|[\u2600-\u26FF]|\u2b05|\u2b06|\u2b07|\u2b1b|\u2b1c|\u2b50|\u2b55|\u231a|\u231b|\u2328|\u23cf|[\u23e9-\u23f3]|[\u23f8-\u23fa]|\ud83c\udccf|\u2934|\u2935|[\u2190-\u21ff])");
        private static readonly Regex _emoteMentionsRegex
            = new Regex(@"<(?:[^\d>]+|:[A-Za-z0-9]+:)\w+>");
        private static readonly Regex _nonAsciiRegex
            = new Regex(@"[^\x00-\x7F]");

        private static Dictionary<ulong, Dictionary<string, object>> _dialogCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        private enum DialogError
        {
            BackgroundNotFound, CharacterNotFound, MessageNotFound,
            MessageTooLong, WrongCharacterSet, Cooldown
        }

        public Dialog() : base()
        {
            var dialogText = Helper.GetLocalization("en").texts.dialog;
            var usage = dialogText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));
            usage = usage.Replace("{characters}", string.Join(", ", _characterString));

            var attr = TypeDescriptor.GetAttributes(typeof(Dialog))
                .OfType<Attributes.CommandAttribute>().FirstOrDefault();
            TypeDescriptor.AddAttributes(typeof(Dialog),
                new Attributes.CommandAttribute("dialog", "fun", usage, null));
        }

        [Command("dialog")]
        public async Task DialogAsync(string background, string character, [Remainder] string content)
        {
            var memberConfig = Helper.GetMemberConfig(Context.User.Id);
            var responseText = Helper.GetLocalization(memberConfig?.Language);
            if (!_dialogCommandTexts.ContainsKey(Context.User.Id))
            {
                _dialogCommandTexts[Context.User.Id] = responseText.texts.dialog;
            }

            if (_characters.Contains(background))
            {
                content = $"{character} {content}";
                character = background;
                background = "camp";
            }

            if (!_backgrounds.Contains(background))
            {
                _ = HandleErrorAsync(Context, DialogError.BackgroundNotFound, background);
                return;
            }
            if (!_characters.Contains(character))
            {
                _ = HandleErrorAsync(Context, DialogError.CharacterNotFound, character);
                return;
            }
            if (string.IsNullOrEmpty(content.Trim()) || content.Length <= 0)
            {
                _ = HandleErrorAsync(Context, DialogError.MessageNotFound, null);
                return;
            }
            if (content.Length > 120)
            {
                _ = HandleErrorAsync(Context, DialogError.MessageTooLong, null);
                return;
            }
            if (_emojiRegex.IsMatch(content) ||
                _emoteMentionsRegex.IsMatch(content) ||
                _nonAsciiRegex.IsMatch(content))
            {
                _ = HandleErrorAsync(Context, DialogError.WrongCharacterSet, null);
                return;
            }

            var dialogCmd = responseText.texts.dialog;
            var dialogErrors = dialogCmd["errors"] as Dictionary<string, object>;

            try
            {
                var stream = await DialogService.GetDialogAsync(Context, new DialogObject
                {
                    background = background,
                    character = character,
                    text = content
                }, dialogErrors["cooldown"].ToString());

                await ReplyAsync(dialogCmd["result"].ToString());
                await Context.Channel.SendFileAsync(stream, "result.png");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                var msg = dialogErrors["generic"].ToString()
                    .Replace("{json}", ex.Message);

                await Context.Channel.SendMessageAsync(msg);

                return;
            }
        }

        public static async Task HandleErrorAsync(ICommandContext context, CommandError error)
        {
            if (!_dialogCommandTexts.ContainsKey(context.User.Id))
            {
                var responseText = Helper
                    .GetLocalization(Helper.GetMemberConfig(context.User.Id)?.Language);
                _dialogCommandTexts[context.User.Id] = responseText.texts.dialog;
            }

            var dialogCmd = _dialogCommandTexts[context.User.Id];
            var dialogErrors = dialogCmd["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                CommandError.BadArgCount => dialogErrors["length_too_short"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await context.Channel.SendMessageAsync(msg);
        }

        private static async Task HandleErrorAsync(ICommandContext context, DialogError error, string input)
        {
            var dialogCmd = _dialogCommandTexts[context.User.Id];
            var dialogErrors = dialogCmd["errors"] as Dictionary<string, object>;

            var backgroundNotFound = dialogErrors["background_not_found"].ToString()
                .Replace("{background}", input)
                .Replace("{backgrounds}", _backgroundString);

            var characterNotFound = dialogErrors["character_not_found"].ToString()
                .Replace("{character}", input)
                .Replace("{characters}", _characterString);

            var msg = error switch
            {
                DialogError.BackgroundNotFound => backgroundNotFound,
                DialogError.CharacterNotFound => characterNotFound,
                DialogError.MessageNotFound => dialogErrors["no_message"].ToString(),
                DialogError.MessageTooLong => dialogErrors["message_too_long"].ToString(),
                DialogError.WrongCharacterSet => dialogErrors["wrong_character_set"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await context.Channel.SendMessageAsync(msg);
        }
    }
}
