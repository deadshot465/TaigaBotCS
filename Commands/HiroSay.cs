using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("hirosay", "fun", null, new[] { "hiro" })]
    public class HiroSay : Dialog
    {
        private enum HiroSayError
        {
            LengthTooShort, BackgroundNotFound, MessageNotFound,
            MessageTooLong, WrongCharacterSet, Cooldown,
            PoseNotFound, ClothesNotFound, FaceNotFound
        }

        private Dictionary<string, object> _userLocalization;
        private string _currentCharacter = string.Empty;

        public HiroSay() : base()
        {
            var hiroSayText = Helper.GetLocalization("en").texts.hirosay;
            var usage = hiroSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(HiroSay),
                new Attributes.CommandAttribute("hirosay", "fun", usage, new[] { "hiro" }));
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(2)]
        public async Task HiroSayAsync()
            => await HandleErrorAsync(HiroSayError.LengthTooShort, string.Empty);

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(4)]
        public async Task HiroSayAsync(string help)
        {
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = "hiro";

            var backgroundStrings = PersistenceService.DialogBackgrounds
                .Select(str => $"`{str}`");

            if (help.Trim().ToLower() == "help")
            {
                var specializationInfo = await DialogService.GetSpecializationInformation("hiro");
                var embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Name = Context.User.Username
                    },
                    Color = new Color(0xff6600),
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

                    embed.Fields.Add(new EmbedFieldBuilder
                    {
                        IsInline = false,
                        Name = facesTitle,
                        Value = allFaces
                    });
                }

                await Context.Channel.SendMessageAsync("Check your DM. <:TaigaCute:514293667507208193>");
                await Context.User.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                await HandleErrorAsync(HiroSayError.LengthTooShort, string.Empty);
            }
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(10)]
        public async Task HiroSayAsync(int pose, string clothes, string face, [Remainder] string content)
            => await HiroSayAsync(PersistenceService
                .DialogBackgrounds[_rng.Next(0, PersistenceService.DialogBackgrounds.Count)], pose, clothes, face, content);

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(7)]
        public async Task HiroSayAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            SetMemberConfig(Context.User.Id);
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = "hiro";

            if (!PersistenceService.DialogBackgrounds.Contains(background))
            {
                _ = HandleErrorAsync(HiroSayError.BackgroundNotFound, background);
                return;
            }

            if (string.IsNullOrEmpty(content.Trim()) || content.Length <= 0)
            {
                _ = HandleErrorAsync(HiroSayError.MessageNotFound, string.Empty);
                return;
            }

            if ((_japaneseRegex.IsMatch(content) && content.Length > 78) || content.Length > 120)
            {
                _ = HandleErrorAsync(HiroSayError.MessageTooLong, string.Empty);
                return;
            }

            if (_emojiRegex.IsMatch(content) ||
                _emoteMentionsRegex.IsMatch(content) ||
                _nonAsciiAndJapaneseRegex.IsMatch(content))
            {
                _ = HandleErrorAsync(HiroSayError.WrongCharacterSet, string.Empty);
                return;
            }

            var specializationInfo = await DialogService.GetSpecializationInformation("hiro");

            if (!specializationInfo.ContainsKey(pose))
            {
                _ = HandleErrorAsync(HiroSayError.PoseNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Clothes"].Contains(clothes))
            {
                _ = HandleErrorAsync(HiroSayError.ClothesNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Faces"].Contains(face))
            {
                _ = HandleErrorAsync(HiroSayError.FaceNotFound, string.Empty);
                return;
            }

            var hiroSayErrors = _userLocalization["errors"] as Dictionary<string, object>;

            try
            {
                var stream = await DialogService.GetDialogAsync(Context, "hiro", new SpecializedDialogObject
                {
                    Background = background,
                    Clothes = clothes,
                    Face = face,
                    IsHiddenCharacter = _currentCharacter == "hiro" ? false : true,
                    Pose = pose,
                    Text = content
                }, hiroSayErrors["cooldown"].ToString());

                if (stream != null)
                {
                    await ReplyAsync(_userLocalization["result"].ToString());
                    await Context.Channel.SendFileAsync(stream, "result.png");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                var msg = hiroSayErrors["generic"].ToString()
                    .Replace("{json}", ex.Message);

                await Context.Channel.SendMessageAsync(msg);

                return;
            }
        }

        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);

            _userLocalization = responseText.texts.hirosay;
        }

        private async Task HandleErrorAsync(HiroSayError error, string message)
        {
            var hiroSayErrors = _userLocalization["errors"] as Dictionary<string, object>;

            var backgroundNotFound = hiroSayErrors["background_not_found"].ToString()
                .Replace("{background}", message)
                .Replace("{backgrounds}", _backgroundString);

            var msg = error switch
            {
                HiroSayError.BackgroundNotFound => backgroundNotFound,
                HiroSayError.LengthTooShort => hiroSayErrors["length_too_short"].ToString(),
                HiroSayError.MessageNotFound => hiroSayErrors["no_message"].ToString(),
                HiroSayError.MessageTooLong => hiroSayErrors["message_too_long"].ToString(),
                HiroSayError.WrongCharacterSet => hiroSayErrors["wrong_character_set"].ToString(),
                HiroSayError.PoseNotFound => hiroSayErrors["pose_not_exist"].ToString(),
                HiroSayError.ClothesNotFound => hiroSayErrors["clothes_not_exist"].ToString(),
                HiroSayError.FaceNotFound => hiroSayErrors["face_not_exist"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
