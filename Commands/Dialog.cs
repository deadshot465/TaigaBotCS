// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        protected readonly string _backgroundString;
        protected readonly string _characterString;

        protected Random _rng = new Random();        

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
            => await DialogAsync(PersistenceService.DialogBackgrounds[_rng.Next(0, PersistenceService.DialogBackgrounds.Count)], character, content);

        [Command("dialog")]
        public async Task DialogAsync(string background, string character, [Remainder] string content)
        {
            SetMemberConfig(Context.User.Id);

            if (PersistenceService.DialogCharacters.Contains(background))
            {
                content = $"{character} {content}";
                character = background;
                background = PersistenceService
                    .DialogBackgrounds[_rng.Next(0, PersistenceService.DialogBackgrounds.Count)];
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
            if ((DialogService.JapaneseRegex.IsMatch(content) && content.Length > 78) || content.Length > 120)
            {
                _ = HandleErrorAsync(DialogError.MessageTooLong, null);
                return;
            }
            if (DialogService.EmojiRegex.IsMatch(content) ||
                DialogService.EmoteMentionsRegex.IsMatch(content) ||
                DialogService.NonASCIIAndJapaneseRegex.IsMatch(content))
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
                    Background = background,
                    Character = character,
                    Text = content
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

        public virtual void SetMemberConfig(ulong userId)
        {
            if (_dialogCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                    .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _dialogCommandTexts[userId] = responseText.texts.dialog;
        }
    }
}
