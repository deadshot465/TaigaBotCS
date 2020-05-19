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
    [Attributes.Command("mhirosay", "fun", null, new[] { "mhiro", "maturehiro", "maturehirosay" })]
    public class MHiroSay : Dialog
    {
        private enum MHiroSayError
        {
            LengthTooShort, BackgroundNotFound, MessageNotFound,
            MessageTooLong, WrongCharacterSet, Cooldown,
            PoseNotFound, ClothesNotFound, FaceNotFound
        }

        private Dictionary<string, object> _userLocalization;
        private string _currentCharacter = string.Empty;

        public MHiroSay() : base()
        {
            var hiroSayText = Helper.GetLocalization("en").texts.hirosay;
            var usage = hiroSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(MHiroSay),
                new Attributes.CommandAttribute("mhirosay", "fun", usage, new[] { "mhiro", "maturehiro", "maturehirosay" }));
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(1)]
        public async Task MHiroSayAsync()
            => await HandleErrorAsync(MHiroSayError.LengthTooShort, string.Empty);

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(3)]
        public async Task MHiroSayAsync(string help)
        {
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = "mhiro";

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

                await Context.Channel.SendMessageAsync("Check your DM. <:chibitaiga:697893400891883531>");
                await Context.User.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                await HandleErrorAsync(MHiroSayError.LengthTooShort, string.Empty);
            }
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(9)]
        public async Task MHiroSayAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            _currentCharacter = "mhiro";
            await MHiroSayAsync(PersistenceService
                .DialogBackgrounds[_rng.Next(0, PersistenceService.DialogBackgrounds.Count)], pose, clothes, face, content);
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(7)]
        public async Task MHiroSayAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            SetMemberConfig(Context.User.Id);
            if (string.IsNullOrEmpty(_currentCharacter)) _currentCharacter = "mhiro";

            if (!PersistenceService.DialogBackgrounds.Contains(background))
            {
                _ = HandleErrorAsync(MHiroSayError.BackgroundNotFound, background);
                return;
            }

            if (string.IsNullOrEmpty(content.Trim()) || content.Length <= 0)
            {
                _ = HandleErrorAsync(MHiroSayError.MessageNotFound, string.Empty);
                return;
            }

            if ((_japaneseRegex.IsMatch(content) && content.Length > 78) || content.Length > 120)
            {
                _ = HandleErrorAsync(MHiroSayError.MessageTooLong, string.Empty);
                return;
            }

            if (_emojiRegex.IsMatch(content) ||
                _emoteMentionsRegex.IsMatch(content) ||
                _nonAsciiAndJapaneseRegex.IsMatch(content))
            {
                _ = HandleErrorAsync(MHiroSayError.WrongCharacterSet, string.Empty);
                return;
            }

            var specializationInfo = await DialogService.GetSpecializationInformation("hiro");

            if (!specializationInfo.ContainsKey(pose))
            {
                _ = HandleErrorAsync(MHiroSayError.PoseNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Clothes"].Contains(clothes))
            {
                _ = HandleErrorAsync(MHiroSayError.ClothesNotFound, string.Empty);
                return;
            }

            if (!specializationInfo[pose]["Faces"].Contains(face))
            {
                _ = HandleErrorAsync(MHiroSayError.FaceNotFound, string.Empty);
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

        private async Task HandleErrorAsync(MHiroSayError error, string message)
        {
            var hiroSayErrors = _userLocalization["errors"] as Dictionary<string, object>;

            var backgroundNotFound = hiroSayErrors["background_not_found"].ToString()
                .Replace("{background}", message)
                .Replace("{backgrounds}", _backgroundString);

            var msg = error switch
            {
                MHiroSayError.BackgroundNotFound => backgroundNotFound,
                MHiroSayError.LengthTooShort => hiroSayErrors["length_too_short"].ToString(),
                MHiroSayError.MessageNotFound => hiroSayErrors["no_message"].ToString(),
                MHiroSayError.MessageTooLong => hiroSayErrors["message_too_long"].ToString(),
                MHiroSayError.WrongCharacterSet => hiroSayErrors["wrong_character_set"].ToString(),
                MHiroSayError.PoseNotFound => hiroSayErrors["pose_not_exist"].ToString(),
                MHiroSayError.ClothesNotFound => hiroSayErrors["clothes_not_exist"].ToString(),
                MHiroSayError.FaceNotFound => hiroSayErrors["face_not_exist"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
