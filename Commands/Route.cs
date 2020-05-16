// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("route", "info", null, null, 5.0f)]
    public class Route : ModuleBase<SocketCommandContext>, ICharacterRoutable, IMemberConfigurable
    {
        private const string _routePath = "./storage/routes.json";
        private readonly List<CharacterObject> _routes;
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

        public Route() : base()
        {
            var rawJson = File.ReadAllText(_routePath);
            _routes = Utf8Json.JsonSerializer.Deserialize<List<CharacterObject>>(rawJson);
        }

        [Command("route")]
        public async Task RouteAsync(params string[] discard)
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
            if (route.name == "Hiro Akiba (Mature)")
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
                var x when x >= 0 && x <= 14 => _routes[0],
                var x when x >= 15 && x <= 19 => _routes[1],
                var x when x >= 20 && x <= 35 => _routes[2],
                var x when x >= 36 && x <= 51 => _routes[3],
                var x when x >= 52 && x <= 67 => _routes[4],
                var x when x >= 68 && x <= 83 => _routes[5],
                var x when x >= 84 && x <= 99 => _routes[6]
            };
        }
    }
}
