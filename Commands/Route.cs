// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("route", "info", null, null, 5.0f)]
    public class Route : ModuleBase<SocketCommandContext>, ICharacterRoutable, IMemberConfigurable
    {
        private const string _kouGif = "https://tetsukizone.com/images/kou.gif";
        private readonly string[] _secretRouteEmoteIds = new[]
        {
            "703591584305774662",
            "710951618576908289",
            "711192310767157248",
            "710957588237385789",
            "711227408933453844"
        };

        private Dictionary<ulong, Dictionary<string, object>> _routeCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private Random _rng = new Random();

        [Command("route")]
        public async Task RouteAsync(params string[] _)
        {
            SetMemberConfig(Context.User.Id);

            var embed = GetEmbeddedMessage();
            await Context.Channel.SendMessageAsync(embed: embed.Item1);
        }

        public Tuple<Embed, string> GetEmbeddedMessage()
        {
            var memberConfig = Helper.GetMemberConfig(Context.User.Id);
            var infos = _routeCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            var route = GetRoute();

            var ending = GetEnding();
            if (route.name == "Hiro Akiba (Mature)" ||
                route.name == "Minamoto Kou")
                ending = (memberConfig?.Language == "en") ? "Perfect" : "パーフェクト";

            var title = infos["next"].ToString()
                .Replace("{name}", route.name)
                .Replace("{ending}", ending);

            var footer = infos["footer"].ToString()
                .Replace("{firstName}", GetFirstName(route.name));

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.User.GetAvatarUrl(ImageFormat.Png),
                    Name = Context.User.Username
                },
                Color = new Color(uint.Parse(route.color, NumberStyles.HexNumber)),
                Description = route.description,
                Fields = new List<EmbedFieldBuilder>
                {
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["age"].ToString(),
                            Value = route.age
                        }
                    },
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["birthday"].ToString(),
                            Value = route.birthday
                        }
                    },
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["animal_motif"].ToString(),
                            Value = route.animal
                        }
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = footer
                },
                ThumbnailUrl = (route.name == "Hiro Akiba (Mature)") ?
                GetEmoteUrl(_secretRouteEmoteIds[_rng.Next(_secretRouteEmoteIds.Length)]) :
                GetEmoteUrl(route.emoteId),
                Title = title
            };

            if (route.name == "Minamoto Kou")
            {
                embed.ThumbnailUrl = _kouGif;
            }

            PersistenceService.AddUserRecord(GetType().Name.ToLower(), route.name, Context.User.Id, $"{ending} Ending");

            return new Tuple<Embed, string>(embed.Build(), string.Empty);
        }

        public string GetEmoteUrl(string emoteId)
            => $"https://cdn.discordapp.com/emojis/{emoteId}.gif?v=1";

        public string GetFirstName(string name)
            => name.Split(' ')[0];

        public void SetMemberConfig(ulong userId)
        {
            if (_routeCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _routeCommandTexts[userId] = responseText.texts.route;
        }

        private string GetEnding()
        {
            var infos = _routeCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            var endings = infos["endings"] as List<object>;
            return endings[_rng.Next(0, endings.Count)].ToString();
        }

        private CharacterObject GetRoute()
        {
            var res = _rng.Next(0, 100);

#pragma warning disable CS8509
            return res switch
            {
                var x when x >= 0 && x <= 14 => PersistenceService.Routes[0],
                var x when x >= 15 && x <= 19 => PersistenceService.Routes[1],
                var x when x >= 20 && x <= 22 => PersistenceService.Routes[7],
                var x when x >= 23 && x <= 38 => PersistenceService.Routes[2],
                var x when x >= 39 && x <= 53 => PersistenceService.Routes[3],
                var x when x >= 54 && x <= 68 => PersistenceService.Routes[4],
                var x when x >= 69 && x <= 83 => PersistenceService.Routes[5],
                var x when x >= 84 && x <= 99 => PersistenceService.Routes[6]
            };
        }
    }
}
