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
    [Attributes.Command("valentine", "info", null, new string[] { "lover", "v" }, 5.0f)]
    public class Valentine : ModuleBase<SocketCommandContext>, ICharacterRoutable, IMemberConfigurable
    {
        private Dictionary<ulong, Dictionary<string, object>> _valentineCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private Random _rng = new Random();

        [Command("valentine")]
        [Alias("lover", "v")]
        public async Task ValentineAsync(params string[] _)
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

            PersistenceService.AddUserRecord(GetType().Name.ToLower(), valentine.name, Context.User.Id);

            return new Tuple<Embed, string>(embed.Build(), prefixSuffix);
        }

        public string GetEmoteUrl(string emoteId)
            => $"https://cdn.discordapp.com/emojis/{emoteId}.png?v=1";

        public string GetFirstName(string name)
        {
            if (name == "Old Lady") return name;
            return name.Split(' ')[0];
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_valentineCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _valentineCommandTexts[userId] = responseText.texts.valentine;
        }

        private CharacterObject GetValentine()
            => PersistenceService.Valentines[_rng.Next(0, PersistenceService.Valentines.Count)];
    }
}
