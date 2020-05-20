using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    public abstract class SpecializedDialogBase : Dialog
    {
        protected enum ConversationError
        {
            LengthTooShort, BackgroundNotFound, MessageNotFound,
            MessageTooLong, WrongCharacterSet, Cooldown,
            PoseNotFound, ClothesNotFound, FaceNotFound
        }

        protected Dictionary<string, object> _userLocalization;
        protected string _currentCharacter = string.Empty;

        public SpecializedDialogBase() : base()
        {
        }

        public virtual async Task SpecializedDialogBaseAsync()
            => await HandleErrorAsync(ConversationError.LengthTooShort, string.Empty);

        public abstract Task SpecializedDialogBaseAsync(string help);

        public async Task SpecializedDialogBaseAsync(string character, string help)
        {
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = character;

            var backgroundStrings = PersistenceService.DialogBackgrounds
                .Select(str => $"`{str}`");

            if (help.Trim().ToLower() == "help")
            {
                var specializationInfo = await DialogService.GetSpecializationInformation(character);
                var textInfo = new CultureInfo("en-us", false).TextInfo;
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Name = Context.User.Username,
                    },
                    Color = new Color(0xff6600),
                    Description = $"Detailed usage for `{textInfo.ToTitleCase(character)}Say`",
                    Fields = new List<EmbedFieldBuilder>
                    {
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "**Backgrounds**",
                                Value = string.Join(", ", backgroundStrings)
                            }
                        },
                        {
                            new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = "**Total Available Poses (0-indexed)**",
                                Value = specializationInfo.Count
                            }
                        }
                    }
                };

                foreach (var item in specializationInfo)
                {
                    var clothesTitle = $"**Available Clothes for Pose {item.Key}**";
                    var facesTitle = $"**Available Faces for Pose {item.Key}**";

                    var formattedClothes = item.Value["Clothes"]
                        .Select(str => $"`{str}`");

                    var formattedFaces = item.Value["Faces"]
                        .Select(str => $"`{str}`");

                    string allClothes = string.Join(", ", formattedClothes);
                    string allFaces = string.Join(", ", formattedFaces);

                    embed.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = false,
                        Name = clothesTitle,
                        Value = allClothes
                    });

                    if (allFaces.Length >= 1024)
                    {
                        var faceMsgList = new List<string>();
                        var lastStart = 0;
                        var stride = 1000;
                        var lastPeriodIndex = 0;

                        do
                        {
                            if (lastStart + stride > allFaces.Length)
                            {
                                faceMsgList.Add(allFaces.Substring(lastStart));
                                break;
                            }
                            lastPeriodIndex = allFaces.Substring(lastStart, lastStart + stride).LastIndexOf(',');
                            var str = allFaces.Substring(lastStart, lastPeriodIndex);
                            faceMsgList.Add(str);
                            lastStart = lastPeriodIndex + 1;

                        } while (true);

                        foreach (var msg in faceMsgList)
                        {
                            embed.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = false,
                                Name = facesTitle,
                                Value = msg
                            });
                        }
                    }
                    else
                    {
                        embed.Fields.Add(new EmbedFieldBuilder
                        {
                            IsInline = false,
                            Name = facesTitle,
                            Value = allFaces
                        });
                    }
                }

                await Context.Channel.SendMessageAsync("Check your DM. <:chibitaiga:697893400891883531>");
                await Context.User.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                await HandleErrorAsync(ConversationError.LengthTooShort, string.Empty);
            }
        }

        public virtual async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
            => await SpecializedDialogBaseAsync(PersistenceService
                .DialogBackgrounds[_rng.Next(0, PersistenceService.DialogBackgrounds.Count)], pose, clothes, face, content);

        public abstract Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content);

        public async Task SpecializedDialogBaseAsync(string character, bool specialized,
            string background, int pose, string clothes, string face, [Remainder] string content)
        {
            SetMemberConfig(Context.User.Id);
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = character;

            if (!PersistenceService.DialogBackgrounds.Contains(background))
            {
                _ = HandleErrorAsync(ConversationError.BackgroundNotFound, background);
                return;
            }

            if (string.IsNullOrEmpty(content.Trim()) || content.Length <= 0)
            {
                _ = HandleErrorAsync(ConversationError.MessageNotFound, string.Empty);
                return;
            }

            if ((DialogService.JapaneseRegex.IsMatch(content) && content.Length > 78) || content.Length > 120)
            {
                _ = HandleErrorAsync(ConversationError.MessageTooLong, string.Empty);
                return;
            }

            if (DialogService.EmojiRegex.IsMatch(content) ||
                DialogService.EmoteMentionsRegex.IsMatch(content) ||
                DialogService.NonASCIIAndJapaneseRegex.IsMatch(content))
            {
                _ = HandleErrorAsync(ConversationError.WrongCharacterSet, string.Empty);
                return;
            }

            var specializationInfo = await DialogService.GetSpecializationInformation(character);

            if (!specializationInfo.ContainsKey(pose))
            {
                _ = HandleErrorAsync(ConversationError.PoseNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Clothes"].Contains(clothes))
            {
                _ = HandleErrorAsync(ConversationError.ClothesNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Faces"].Contains(face))
            {
                _ = HandleErrorAsync(ConversationError.FaceNotFound, string.Empty);
                return;
            }

            var errors = _userLocalization["errors"] as Dictionary<string, object>;
            
            try
            {
                var stream = await DialogService.GetDialogAsync(Context, character, new SpecializedDialogObject
                {
                    Background = background,
                    Clothes = clothes,
                    Face = face,
                    IsHiddenCharacter = specialized,
                    Pose = pose,
                    Text = content
                }, errors["cooldown"].ToString());

                if (stream != null)
                {
                    await ReplyAsync(_userLocalization["result"].ToString());
                    await Context.Channel.SendFileAsync(stream, "result.png");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                var msg = errors["generic"].ToString()
                    .Replace("{json}", ex.Message);

                await Context.Channel.SendMessageAsync(msg);
                return;
            }
        }

        public override abstract void SetMemberConfig(ulong userId);

        protected async Task HandleErrorAsync(ConversationError error, string input)
        {
            var errorMsg = _userLocalization["errors"] as Dictionary<string, object>;

            var backgroundNotFound = errorMsg["background_not_found"].ToString()
                .Replace("{background}", input)
                .Replace("{backgrounds}", _backgroundString);

            var msg = error switch
            {
                ConversationError.BackgroundNotFound => backgroundNotFound,
                ConversationError.LengthTooShort => errorMsg["length_too_short"].ToString(),
                ConversationError.MessageNotFound => errorMsg["no_message"].ToString(),
                ConversationError.MessageTooLong => errorMsg["message_too_long"].ToString(),
                ConversationError.WrongCharacterSet => errorMsg["wrong_character_set"].ToString(),
                ConversationError.PoseNotFound => errorMsg["pose_not_exist"].ToString(),
                ConversationError.ClothesNotFound => errorMsg["clothes_not_exist"].ToString(),
                ConversationError.FaceNotFound => errorMsg["face_not_exist"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
