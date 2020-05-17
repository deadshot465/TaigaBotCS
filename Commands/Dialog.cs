// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("dialog", "fun", null, null)]
    public class Dialog : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        public DialogService DialogService { get; set; }

        private readonly string _backgroundString;
        private readonly string _characterString;
        private readonly Regex _emojiRegex
            = new Regex(@"(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff]|[\u0023-\u0039]\ufe0f?\u20e3|\u3299|\u3297|\u303d|\u3030|\u24c2|\ud83c[\udd70-\udd71]|\ud83c[\udd7e-\udd7f]|\ud83c\udd8e|\ud83c[\udd91-\udd9a]|\ud83c[\udde6-\uddff]|\ud83c[\ude01-\ude02]|\ud83c\ude1a|\ud83c\ude2f|\ud83c[\ude32-\ude3a]|\ud83c[\ude50-\ude51]|\u203c|\u2049|[\u25aa-\u25ab]|\u25b6|\u25c0|[\u25fb-\u25fe]|\u00a9|\u00ae|\u2122|\u2139|\ud83c\udc04|[\u2600-\u26FF]|\u2b05|\u2b06|\u2b07|\u2b1b|\u2b1c|\u2b50|\u2b55|\u231a|\u231b|\u2328|\u23cf|[\u23e9-\u23f3]|[\u23f8-\u23fa]|\ud83c\udccf|\u2934|\u2935|[\u2190-\u21ff])");
        private readonly Regex _emoteMentionsRegex
            = new Regex(@"<(?:[^\d>]+|:[A-Za-z0-9]+:)\w+>");
        private readonly Regex _japaneseRegex
            = new Regex(@"[\u4e00-\u9fbf\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u3000-\u303f]");
        private readonly Regex _nonAsciiAndJapaneseRegex
            = new Regex(@"[^\x00-\x7F\u4e00-\u9fbf\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u3000-\u303f\u2018-\u2019]");

        private Dictionary<ulong, Dictionary<string, object>> _dialogCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        private enum DialogError
        {
            LengthTooShort, BackgroundNotFound, CharacterNotFound, MessageNotFound,
            MessageTooLong, WrongCharacterSet, Cooldown
        }

        public Dialog() : base()
        {
            _backgroundString = string.Join(", ", PersistenceService.DialogBackgrounds);
            _characterString = string.Join(", ", PersistenceService.DialogCharacters);

            var dialogText = Helper.GetLocalization("en").texts.dialog;
            var usage = dialogText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));
            usage = usage.Replace("{characters}", string.Join(", ", _characterString));

            TypeDescriptor.AddAttributes(typeof(Dialog),
                new Attributes.CommandAttribute("dialog", "fun", usage, null));
        }

#pragma warning disable CS1998
        [Command("dialog")]
        public async Task DialogAsync()
            => _ = HandleErrorAsync(DialogError.LengthTooShort, null);

        [Command("dialog")]
        public async Task DialogAsync(string input)
            => _ = HandleErrorAsync(DialogError.LengthTooShort, null);

        [Command("dialog")]
        public async Task DialogAsync(string character, string content)
            => await DialogAsync("camp", character, content);

        [Command("dialog")]
        public async Task DialogAsync(string background, string character, [Remainder] string content)
        {
            SetMemberConfig(Context.User.Id);

            if (PersistenceService.DialogCharacters.Contains(background))
            {
                content = $"{character} {content}";
                character = background;
                background = "camp";
            }

            if (!PersistenceService.DialogBackgrounds.Contains(background))
            {
                _ = HandleErrorAsync(DialogError.BackgroundNotFound, background);
                return;
            }
            if (!PersistenceService.DialogCharacters.Contains(character))
            {
                _ = HandleErrorAsync(DialogError.CharacterNotFound, character);
                return;
            }
            if (string.IsNullOrEmpty(content.Trim()) || content.Length <= 0)
            {
                _ = HandleErrorAsync(DialogError.MessageNotFound, null);
                return;
            }
            if ((_japaneseRegex.IsMatch(content) && content.Length > 78) || content.Length > 120)
            {
                _ = HandleErrorAsync(DialogError.MessageTooLong, null);
                return;
            }
            if (_emojiRegex.IsMatch(content) ||
                _emoteMentionsRegex.IsMatch(content) ||
                _nonAsciiAndJapaneseRegex.IsMatch(content))
            {
                _ = HandleErrorAsync(DialogError.WrongCharacterSet, null);
                return;
            }

            var dialogCmd = _dialogCommandTexts[Context.User.Id];
            var dialogErrors = dialogCmd["errors"] as Dictionary<string, object>;

            try
            {
                var stream = await DialogService.GetDialogAsync(Context, new DialogObject
                {
                    background = background,
                    character = character,
                    text = content
                }, dialogErrors["cooldown"].ToString());

                if (stream != null)
                {
                    await ReplyAsync(dialogCmd["result"].ToString());
                    await Context.Channel.SendFileAsync(stream, "result.png");
                }
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

        private async Task HandleErrorAsync(DialogError error, string input)
        {
            SetMemberConfig(Context.User.Id);
            var dialogCmd = _dialogCommandTexts[Context.User.Id];
            var dialogErrors = dialogCmd["errors"] as Dictionary<string, object>;

            var backgroundNotFound = dialogErrors["background_not_found"].ToString()
                .Replace("{background}", input)
                .Replace("{backgrounds}", _backgroundString);

            var characterNotFound = dialogErrors["character_not_found"].ToString()
                .Replace("{character}", input)
                .Replace("{characters}", _characterString);

            var msg = error switch
            {
                DialogError.LengthTooShort => dialogErrors["length_too_short"].ToString(),
                DialogError.BackgroundNotFound => backgroundNotFound,
                DialogError.CharacterNotFound => characterNotFound,
                DialogError.MessageNotFound => dialogErrors["no_message"].ToString(),
                DialogError.MessageTooLong => dialogErrors["message_too_long"].ToString(),
                DialogError.WrongCharacterSet => dialogErrors["wrong_character_set"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_dialogCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                    .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _dialogCommandTexts[userId] = responseText.texts.dialog;
        }
    }
}
