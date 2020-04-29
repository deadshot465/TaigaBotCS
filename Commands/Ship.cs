// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("ship", "fun", null, null)]
    public class Ship : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        public struct ShipMessage
        {
            public int max_score;
            public string message;
        }

        public ShipService ShipService { get; set; }

        private const string _riggedPairPath = "./storage/riggedPairs.json";
        private const string _shipMessagePath = "./storage/shipMessages.json";
        private const string _kouEmotePath = "https://cdn.discordapp.com/emojis/700119260394946620.png";
        private const string _hiroEmotePath = "https://cdn.discordapp.com/emojis/704022326412443658.png";
        private const string _kouName = "Minamoto Kou";
        private const string _hiroName = "Akiba Hiro";
        private readonly List<List<ulong>> _riggedPairs;
        private readonly List<ShipMessage> _shipMessages;

        private Dictionary<ulong, Dictionary<string, object>> _shipCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();

        private enum ShipError
        {
            LengthTooShort, UserNotFound, SelfMatch
        }

        public Ship() : base()
        {
            if (File.Exists(_riggedPairPath))
            {
                var rawJson = File.ReadAllText(_riggedPairPath);
                _riggedPairs = Utf8Json.JsonSerializer.Deserialize<List<List<ulong>>>(rawJson);
            }

            var rawShipMessages = File.ReadAllText(_shipMessagePath);
            _shipMessages = Utf8Json.JsonSerializer.Deserialize<List<ShipMessage>>(rawShipMessages);
        }

#pragma warning disable CS1998
        [Command("ship")]
        public async Task ShipAsync()
            => _ = HandleErrorAsync(ShipError.LengthTooShort, null);

        [Command("ship")]
        public async Task ShipAsync(string userName1)
            => _ = HandleErrorAsync(ShipError.LengthTooShort, null);

        [Command("ship")]
        public async Task ShipAsync(string userName1, string userName2)
        {
            SetMemberConfig(Context.User.Id);

            if (userName1.ToLower().Contains("kou") &&
                userName2.ToLower().Contains("hiro"))
            {
                await ShipSecretRomance();
                return;
            }
            else if (userName1.ToLower().Contains("hiro") &&
                userName2.ToLower().Contains("kou"))
            {
                await ShipSecretRomance(_hiroName, _kouName);
                return;
            }

            IGuildUser target1 = null;
            IGuildUser target2 = null;
            
            var searchResult = Helper.SearchUser(Context.Guild, userName1);
            if (searchResult != null && searchResult.Length > 0)
                target1 = searchResult[0];
            else
            {
                await HandleErrorAsync(ShipError.UserNotFound, userName1);
                return;
            }

            searchResult = Helper.SearchUser(Context.Guild, userName2);
            target2 = FindNextUser(target1, searchResult);

            if (target2 == null)
            {
                await HandleErrorAsync(ShipError.UserNotFound, userName2);
                return;
            }

            await ShipAsync(target1, target2);
        }

        [Command("ship")]
        public async Task ShipAsync(IGuildUser user1, IGuildUser user2)
        {
            SetMemberConfig(Context.User.Id);
            var infos = _shipCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            
            var shipData = CalculcateScore(user1, user2);
            var (score, scoreMessage) = shipData;
            var img1 = user1.GetAvatarUrl(ImageFormat.Png);
            var img2 = user2.GetAvatarUrl(ImageFormat.Png);

            var response = await ShipService.GetShipAsync(img1, img2);

            var embed = new EmbedBuilder
            {
                Title = infos["title"].ToString().Replace("{user1}", user1.Username)
                .Replace("{user2}", user2.Username),
                Fields = new List<EmbedFieldBuilder>
                {
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = infos["score"].ToString().Replace("{score}", score.ToString()),
                            Value = scoreMessage.Replace("{name}", user1.Nickname ?? user1.Username)
                            .Replace("{name2}", user2.Nickname ?? user2.Username)
                        }
                    }
                },
            };

            await Context.Channel.SendMessageAsync(embed: embed.Build());
            await Context.Channel.SendFileAsync(response, "love.png");
        }

        [Command("ship")]
        public async Task ShipAsync(string userName1, string userName2, params string[] rest)
            => await ShipAsync(userName1, userName2);

        public void SetMemberConfig(ulong userId)
        {
            if (_shipCommandTexts.ContainsKey(userId)) return;
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _shipCommandTexts[userId] = responseText.texts.ship;
        }

        private Tuple<int, string> CalculcateScore(IGuildUser user1, IGuildUser user2)
        {
            var shipErrors = _shipCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var score = (int)((user1.Id + user2.Id) / 7 % 100);

            return (user1.Id == user2.Id) ?
                new Tuple<int, string>(100, shipErrors["self_match"].ToString()) :
                new Tuple<int, string>(score, FindMessage(score));
        }

        private string FindMessage(int score)
            => _shipMessages.Find(obj => score <= obj.max_score)!.message;

        private IGuildUser FindNextUser(IGuildUser firstUser, IEnumerable<IGuildUser> seconds)
        {
            if (seconds == null || seconds.Count() <= 0) return null;
            if (seconds.Count() == 1) return seconds.First();

            foreach (var user in seconds)
            {
                if (user.Id == firstUser.Id) continue;
                return user;
            }

            return null;
        }

        private async Task HandleErrorAsync(ShipError error, string userName)
        {
            SetMemberConfig(Context.User.Id);
            var shipErrors = _shipCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var userNotFound = shipErrors["user_not_found"].ToString()
                .Replace("{user}", userName);

            var msg = error switch
            {
                ShipError.LengthTooShort => shipErrors["length_too_short"].ToString(),
                ShipError.UserNotFound => userNotFound,
                ShipError.SelfMatch => shipErrors["self_match"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }

        private async Task ShipSecretRomance(string first = _kouName, string second = _hiroName)
        {
            var infos = _shipCommandTexts[Context.User.Id]["infos"] as Dictionary<string, object>;
            var (score, scoreMessage) = new Tuple<int, string>(10000,
                $"What are you talking about? {first} and {second} of course are the cutest two!");

            var response = await ShipService.GetShipAsync(first == _kouName ? _kouEmotePath : _hiroEmotePath,
                second == _hiroName ? _hiroEmotePath : _kouEmotePath);

            var embed = new EmbedBuilder
            {
                Title = infos["title"].ToString().Replace("{user1}", (first == _kouName) ? _kouName : _hiroName)
                .Replace("{user2}", (second == _hiroName) ? _hiroName : _kouName),
                Fields = new List<EmbedFieldBuilder>
                {
                    {
                        new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = infos["score"].ToString().Replace("{score}", score.ToString()),
                            Value = scoreMessage.Replace("{name}", first)
                            .Replace("{name2}", second)
                        }
                    }
                },
            };

            await Context.Channel.SendMessageAsync(embed: embed.Build());
            await Context.Channel.SendFileAsync(response, "love.png");
        }
    }
}
