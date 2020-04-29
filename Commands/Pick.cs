// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("pick", "util", null, new string[] { "choose" })]
    public class Pick : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        private Dictionary<ulong, Dictionary<string, object>> _pickCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private Random _rng = new Random();

        private enum PickError
        {
            LengthTooShort
        }

#pragma warning disable CS1998
        [Command("pick")]
        [Alias("choose")]
        public async Task PickAsync()
            => _ = HandleErrorAsync(PickError.LengthTooShort);

        [Command("pick")]
        [Alias("choose")]
        public async Task PickAsync([Remainder] string options)
        {
            SetMemberConfig(Context.User.Id);

            var optionList = options.Split('|').Select(str => str.Trim()).ToList();
            var msg = _pickCommandTexts[Context.User.Id]["result"].ToString()
                .Replace("{option}", optionList[_rng.Next(0, optionList.Count)]);

            await ReplyAsync(msg);
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_pickCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper.
                GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _pickCommandTexts[userId] = responseText.texts.pick;
        }

        private async Task HandleErrorAsync(PickError error)
        {
            SetMemberConfig(Context.User.Id);
            var pickErrors = _pickCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                PickError.LengthTooShort => pickErrors["length_too_short"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
