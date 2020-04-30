// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using Owoify;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("owoify", "fun", null, null)]
    public class Owoify : ModuleBase<SocketCommandContext>
    {
        private readonly Regex _emoteIdRegex
            = new Regex(@"[^:]+(?=>)");
        private readonly string[] _intensities = new[]
        {
            "easy", "medium", "hard"
        };

        private enum OwoifyError
        {
            LengthTooShort, LengthTooLong
        }

        [Command("owoify")]
        public async Task OwoifyAsync()
            => await HandleErrorAsync(OwoifyError.LengthTooShort);

        [Command("owoify")]
        public async Task OwoifyAsync(string intensity, params string[] text)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);
            var owoifyCmd = responseText.texts.owoify;

            var level = intensity.ToLower().Trim() switch
            {
                "medium" => Owoifier.OwoifyLevel.Uwu,
                "hard" => Owoifier.OwoifyLevel.Uvu,
                _ => Owoifier.OwoifyLevel.Owo,
            };

            if (text.Length > 1000)
            {
                await HandleErrorAsync(OwoifyError.LengthTooLong);
                return;
            }

            var msg = new List<string>();

            if (!_intensities.Contains(intensity.ToLower().Trim()))
                msg.Add(intensity);

            foreach (var t in text)
            {
                if (_emoteIdRegex.IsMatch(t))
                    msg.Add(t);
                else
                    msg.Add(Owoifier.Owoify(t, level));
            }

            var result = owoifyCmd["result"].ToString()
                .Replace("{author}", Context.User.Username)
                .Replace("{text}", string.Join(' ', msg));

            await Context.Channel.SendMessageAsync(result);
        }

        private async Task HandleErrorAsync(OwoifyError error)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);
            var owoifyCmd = responseText.texts.owoify;
            var owoifyErrors = owoifyCmd["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                OwoifyError.LengthTooShort => owoifyErrors["length_too_short"].ToString(),
                OwoifyError.LengthTooLong => owoifyErrors["length_too_long"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
