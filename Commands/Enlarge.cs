using Discord.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("enlarge", "util", "enlarge", null)]
    public class Enlarge : ModuleBase<SocketCommandContext>
    {
        private static readonly Regex _emoteIdRegex
            = new Regex(@"[^:]+(?=>)");

        private static readonly Regex _emoteIsAnimatedRegex
            = new Regex(@"(<a)");

        [Command("enlarge")]
        public async Task EnlargeAsync(string emote)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);
            var enlargeCmd = responseText.texts.enlarge;

            if (!_emoteIdRegex.IsMatch(emote))
            {
                await Context.Channel
                    .SendMessageAsync((enlargeCmd["errors"] as Dictionary<string, object>)["no_emote"].ToString());
                return;
            }

            var emoteId = _emoteIdRegex.Match(emote).Groups[0].Value;
            var emoteFormat = _emoteIsAnimatedRegex.IsMatch(emote) ? ".gif" : ".png";
            var emoteLink = $"https://cdn.discordapp.com/emojis/{emoteId}{emoteFormat}";

            await Context.Channel.SendMessageAsync(emoteLink);
        }

        public static async Task HandleErrorAsync(ICommandContext context, CommandError error)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(context.User.Id)?.Language);
            var enlargeCmd = responseText.texts.enlarge;

            await context.Channel
                    .SendMessageAsync((enlargeCmd["errors"] as Dictionary<string, object>)["no_emote"].ToString());
        }
    }
}
