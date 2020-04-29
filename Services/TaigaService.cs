// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Services
{
    public class TaigaService
    {
        public struct RandomMessageObject
        {
            public string keyword;
            public Dictionary<string, string[]> messages;
            public string[] reactions;
        }

        private const string RANDOM_MESSAGE_PATH = "./storage/randomMessages.json";

        private readonly IServiceProvider _services;
        private readonly DialogService _dialogService;
        private readonly ImageService _imageService;

        private readonly ulong _taigaId;
        private readonly double _mentionReactionChance;
        private readonly double _reactionChance;
        private readonly double _randomReplyChance;
        private readonly double _specializedChance;
        private readonly List<RandomMessageObject> _randomMessages;
        private readonly Regex _reactionRegex = new Regex(@"<:\w+:\d+>");
        private readonly string[] _backgrounds = new[]
        {
            "bath", "beach", "cabin", "camp",
            "cave", "forest", "messhall"
        };

        private Random _rng = new Random();
        private Dictionary<ulong, string> _randomMessageUserLang
            = new Dictionary<ulong, string>();

        public TaigaService(IServiceProvider services)
        {
            _dialogService = services.GetRequiredService<DialogService>();
            _imageService = services.GetRequiredService<ImageService>();
            _services = services;

            _taigaId = ulong.Parse(DotNetEnv.Env.GetString("BOT_ID"));
            _mentionReactionChance = DotNetEnv.Env.GetDouble("MENTION_REACTION_CHANCE");
            _reactionChance = DotNetEnv.Env.GetInt("REACTION_CHANCE");
            _randomReplyChance = DotNetEnv.Env.GetInt("RDM_REPLY_CHANCE");
            _specializedChance = DotNetEnv.Env.GetInt("SPECIALIZED_CHANCE");

            var rawJson = File.ReadAllText(RANDOM_MESSAGE_PATH);
            _randomMessages = Utf8Json.JsonSerializer
                .Deserialize<List<RandomMessageObject>>(rawJson);
        }

#pragma warning disable CS1998
        public async Task HandleMessageAsync(SocketUserMessage message)
        {
            SetMemberConfig(message.Author.Id);
            _ = HandleMentions(message);
            _ = HandleReactions(message);
            _ = HandleReplies(message);
        }

        /// <summary>
        /// React to mentions
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>An awaitable task.</returns>
        private async Task HandleMentions(SocketUserMessage message)
        {
            if (message.MentionedUsers.Where(u => u.Id == _taigaId).Count() <= 0)
                return;
            if (!HitOrMiss(_mentionReactionChance)) return;

            var msgObj = _randomMessages.Where(msg => msg.keyword == "taiga").First();
            var responses = _randomMessageUserLang[message.Author.Id] == "en" ?
                    msgObj.messages["en"] : msgObj.messages["jp"];
            await message.Channel.SendMessageAsync(responses[_rng.Next(0, responses.Length)]);
        }

        /// <summary>
        /// React to messages.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>An awaitable task.</returns>
        private async Task HandleReactions(SocketUserMessage message)
        {
            if (message.Author.IsBot) return;
            if (!HitOrMiss(_reactionChance)) return;

            foreach (var msgObj in _randomMessages)
            {
                if (!message.Content.ToLower().Contains(msgObj.keyword)) continue;

                var reaction = msgObj.reactions[_rng.Next(0, msgObj.reactions.Length)];

                if (_reactionRegex.IsMatch(reaction))
                    await message.AddReactionAsync(Emote.Parse(reaction));
                else
                {
                    var emoji = new Emoji(reaction);
                    await message.AddReactionAsync(emoji);
                }

                break;
            }
        }

        /// <summary>
        /// Replies to a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>An awaitable task.</returns>
        private async Task HandleReplies(SocketUserMessage message)
        {
            var argPos = 0;
            if (message.Author.IsBot) return;
            if (message.HasStringPrefix(DotNetEnv.Env.GetString("PREFIX"), ref argPos)) return;

            var content = message.Content.ToLower();

            // If the content doesn't contain any keyword, we don't bother rolling the dice
            var shouldReply = _randomMessages
                .Any(msgObj => content.Contains(msgObj.keyword));
            
            if (!shouldReply) return;
            if (!HitOrMiss(_randomReplyChance)) return;

            if (HitOrMiss(_specializedChance))
            {
                // Hiro and Aiden will have even more different specialized reactions
                if (content.Contains("hiro"))
                {
                    try
                    {
                        var stream = await _dialogService.GetDialogAsync(null, new DialogObject
                        {
                            background = _backgrounds[_rng.Next(0, _backgrounds.Length)],
                            character = "taiga",
                            text = "Hiro will be terribly wrong if he thinks he can steal Keitaro from me!"
                        }, string.Empty);

                        if (stream != null)
                            await message.Channel.SendFileAsync(stream, "result.png");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex.Message);
                        throw;
                    }
                }
                else if (content.Contains("aiden"))
                {
                    var stream = await _imageService.GetImageAsync(null, "hamburger", string.Empty);
                    
                    if (stream != null)
                    {
                        await message.Channel.SendMessageAsync("Three orders of double-quarter-pounder cheeseburgers! Two large fries and one large soda!\n" +
                            "Burger patties well-done, three slices of pickles for each! No mayonnaise! Just ketchup and mustard!");
                        await message.Channel.SendFileAsync(stream, "burger.jpg");
                    }
                }
            }
            else
            {
                foreach (var msgObj in _randomMessages)
                {
                    if (!content.Contains(msgObj.keyword)) continue;

                    var responses = _randomMessageUserLang[message.Author.Id] == "en" ?
                        msgObj.messages["en"] : msgObj.messages["jp"];

                    var msg = responses[_rng.Next(0, responses.Length)];
                    await message.Channel.SendMessageAsync(msg);
                    break;
                }
            }
        }

        private bool HitOrMiss(double chance)
            => _rng.Next(0, 100) < chance;

        public void SetMemberConfig(ulong userId)
        {
            if (_randomMessageUserLang.ContainsKey(userId)) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _randomMessageUserLang[userId] = responseText.lang;
        }
    }
}
