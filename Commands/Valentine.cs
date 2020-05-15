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
using Utf8Json;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("valentine", "info", null, new string[] { "lover", "v" }, 5.0f)]
    public class Valentine : ModuleBase<SocketCommandContext>, ICharacterRoutable, IMemberConfigurable
    {
        private const string _valentinePath = "./storage/valentines.json";
        private readonly List<CharacterObject> _valentines;

        private Dictionary<ulong, Dictionary<string, object>> _valentineCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private Random _rng = new Random();

        public Valentine() : base()
        {
            var rawJson = File.ReadAllText(_valentinePath);
            _valentines = JsonSerializer.Deserialize<List<CharacterObject>>(rawJson);
        }

        [Command("valentine")]
        [Alias("lover", "v")]
        public async Task ValentineAsync(params string[] discard)
        {
            SetMemberConfig(Context.User.Id);
            var infos = _valentineCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            var embed = GetEmbeddedMessage();

            if (!string.IsNullOrEmpty(embed.Item2))
            {
                await Context.Channel.SendMessageAsync(infos["keitaro_header"].ToString())
                    .ContinueWith(task => Context.Channel.SendMessageAsync(embed: embed.Item1));
            }
            else
                await Context.Channel.SendMessageAsync(embed: embed.Item1);
        }

        public Tuple<Embed, string> GetEmbeddedMessage()
        {
            var infos = _valentineCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            var valentine = GetValentine();
            var isKeitaro = GetFirstName(valentine.name) == "Keitaro";
            var prefixSuffix = isKeitaro ? "~~" : string.Empty;
            var footer = isKeitaro ?
                infos["keitaro_footer"].ToString() :
                infos["normal_footer"].ToString().Replace("{firstName}", GetFirstName(valentine.name));

            var valentineName = infos["valentine"].ToString()
                .Replace("{name}", valentine.name)
                .Replace("{prefixSuffix}", prefixSuffix);

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = Context.User.GetAvatarUrl(ImageFormat.Png),
                    Name = Context.User.Username
                },
                Color = new Color(uint.Parse(valentine.color, NumberStyles.HexNumber)),
                Description = $"{prefixSuffix}{valentine.description}{prefixSuffix}",
                Fields = new List<EmbedFieldBuilder>
                {
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["age"].ToString(),
                            Value = $"{prefixSuffix}{valentine.age}{prefixSuffix}"
                        }
                    },
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["birthday"].ToString(),
                            Value = $"{prefixSuffix}{valentine.birthday}{prefixSuffix}"
                        }
                    },
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = true,
                            Name = infos["animal_motif"].ToString(),
                            Value = $"{prefixSuffix}{valentine.animal}{prefixSuffix}"
                        }
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = footer
                },
                ThumbnailUrl = GetEmoteUrl(valentine.emoteId),
                Title = valentineName
            };

            return new Tuple<Embed, string>(embed.Build(), prefixSuffix);
        }

        public string GetEmoteUrl(string emoteId)
            => $"https://cdn.discordapp.com/emojis/{emoteId}.png?v=1";

        public string GetFirstName(string name)
            => name.Split(' ')[0];

        public void SetMemberConfig(ulong userId)
        {
            if (_valentineCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _valentineCommandTexts[userId] = responseText.texts.valentine;
        }

        private CharacterObject GetValentine()
            => _valentines[_rng.Next(0, _valentines.Count)];
    }
}
