// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;
using Utf8Json;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("oracle", "info", null, new string[] { "fortune" })]
    public class Oracle : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        public struct OracleObject
        {
            public int no;
            public string fortune;
            public string meaning;
            public string content;
        }

        private const string _thumbnailUrl = "https://cdn.discordapp.com/emojis/701918026164994049.png?v=1";
        private const string _oraclePath = "./storage/oracles.json";
        private Dictionary<ulong, Dictionary<string, object>> _oracleCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private readonly List<OracleObject> _oracles;

        private Random _rng = new Random();

        public Oracle() : base()
        {
            var rawJson = File.ReadAllText(_oraclePath);
            _oracles = JsonSerializer.Deserialize<List<OracleObject>>(rawJson);
        }

        [Command("oracle")]
        [Alias("fortune")]
        public async Task OracleAsync()
        {
            SetMemberConfig(Context.User.Id);
            var uiTexts = _oracleCommandTexts[Context.User.Id]["uis"] as Dictionary<string, object>;
            var oracle = GetOracle();

            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = Context.User.Username,
                    IconUrl = Context.User.GetAvatarUrl(ImageFormat.Png)
                },
                Color = new Color(0xff0000),
                Fields = new List<EmbedFieldBuilder>
                {
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = uiTexts["no"].ToString(),
                            Value = oracle.no
                        }
                    },
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = uiTexts["meaning"].ToString(),
                            Value = oracle.meaning
                        }
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = _oracleCommandTexts[Context.User.Id]["result"].ToString()
                },
                Description = oracle.content,
                ThumbnailUrl = _thumbnailUrl,
                Title = oracle.fortune
            };

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("oracle")]
        [Alias("fortune")]
        public async Task OracleAsync(params string[] rest)
            => await OracleAsync();

        public void SetMemberConfig(ulong userId)
        {
            if (_oracleCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _oracleCommandTexts[userId] = responseText.texts.oracle;
        }

        private OracleObject GetOracle()
            => _oracles[_rng.Next(0, _oracles.Count)];
    }
}
