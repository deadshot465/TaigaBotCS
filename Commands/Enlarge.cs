// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("enlarge", "util", null, null)]
    public class Enlarge : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        private readonly Regex _emoteIdRegex
            = new Regex(@"[^:]+(?=>)");

        private readonly Regex _emoteIsAnimatedRegex
            = new Regex(@"(<a)");

        private Dictionary<ulong, Dictionary<string, object>> _enlargeCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        [Command("enlarge")]
        public async Task EnlargeAsync()
        {
            SetMemberConfig(Context.User.Id);
            var enlargeCmd = _enlargeCommandTexts[Context.User.Id];
            var enlargeErrors = enlargeCmd["errors"] as Dictionary<string, object>;

            await Context.Channel
                    .SendMessageAsync(enlargeErrors["no_emote"].ToString());
        }

        [Command("enlarge")]
        public async Task EnlargeAsync(string emote)
        {
            SetMemberConfig(Context.User.Id);
            var enlargeCmd = _enlargeCommandTexts[Context.User.Id];
            var enlargeErrors = enlargeCmd["errors"] as Dictionary<string, object>;

            if (!_emoteIdRegex.IsMatch(emote))
            {
                await Context.Channel
                    .SendMessageAsync(enlargeErrors["no_emote"].ToString());
                return;
            }

            var matches = _emoteIdRegex.Matches(emote);
            var animatedMatches = _emoteIsAnimatedRegex.Matches(emote);

            if ((matches.Count > 0 && animatedMatches.Count > 0) &&
                matches.Count != animatedMatches.Count)
            {
                await Context.Channel
                    .SendMessageAsync("Please either send emotes with the same format (static or animated), or separate them with space.");
                return;
            }

            for (var i = 0; i < matches.Count; i++)
            {
                for (var j = 0; j < matches[i].Groups.Count; j++)
                {
                    var emoteId = matches[i].Groups[j].Value;
                    var emoteFormat = animatedMatches.Count > 0 ? ".gif" : ".png";
                    var emoteLink = $"https://cdn.discordapp.com/emojis/{emoteId}{emoteFormat}";
                    
                    await Context.Channel.SendMessageAsync(emoteLink);
                }
            }
        }

        [Command("enlarge")]
        public async Task EnlargeAsync(string emote, params string[] rest)
        {
            SetMemberConfig(Context.User.Id);
            var enlargeCmd = _enlargeCommandTexts[Context.User.Id];
            var enlargeErrors = enlargeCmd["errors"] as Dictionary<string, object>;

            if (!_emoteIdRegex.IsMatch(emote))
            {
                await Context.Channel
                    .SendMessageAsync(enlargeErrors["no_emote"].ToString());
                return;
            }

            var match = _emoteIdRegex.Match(emote);
            var emoteId = match.Groups[0].Value;
            var emoteFormat = _emoteIsAnimatedRegex.IsMatch(emote) ? ".gif" : ".png";
            var emoteLink = $"https://cdn.discordapp.com/emojis/{emoteId}{emoteFormat}";

            await Context.Channel.SendMessageAsync(emoteLink);

            for (var i = 0; i < rest.Length; i++)
            {
                if (!_emoteIdRegex.IsMatch(rest[i]))
                    continue;

                match = _emoteIdRegex.Match(rest[i]);
                emoteId = match.Groups[0].Value;
                emoteFormat = _emoteIsAnimatedRegex.IsMatch(rest[i]) ? ".gif" : ".png";
                emoteLink = $"https://cdn.discordapp.com/emojis/{emoteId}{emoteFormat}";

                await Context.Channel.SendMessageAsync(emoteLink);
            }
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_enlargeCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _enlargeCommandTexts[userId] = responseText.texts.enlarge;
        }
    }
}
