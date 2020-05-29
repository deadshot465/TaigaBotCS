using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("comic", "fun", null, new[] { "manga" }, 10.0f)]
    public class Comic : ModuleBase<SocketCommandContext>, IErrorHandler
    {
        public DialogService DialogService { get; set; }

        private readonly Regex _specializedDialogRegex
            = new Regex(@"([a-z]+)\s{1}([a-z]+)\s{1}(\d{1})\s{1}([\w]+)\s{1}([\w]+)\s{1}(.*)");

        private readonly Regex _dialogRegex
            = new Regex(@"([a-z]+)\s{1}([a-z]+)\s{1}([a-z]+)\s{1}(.*)");

        private readonly Dictionary<string, string> _availableSpecializations
            = new Dictionary<string, string>
            {
                { "hirosay", "hiro" },
                { "mhirosay", "hiro" },
                { "taigasay", "taiga" },
                { "keitarosay", "keitaro" },
                { "yoichisay", "yoichi" },
                { "yurisay", "yuri" }
            };

        private enum ComicError
        {
            InvalidCommand, MessageTooShort, NoSuchSpecialization,
            NoSuchBackground, NoSuchPose, NoSuchCloth, NoSuchFace,
            NoSuchCharacter, NoAttachment, MessageTooLong,
            WrongCharacterSet, TooManyImages
        }

        [Command("comic")]
        [Alias("manga")]
        [Priority(10)]
        public async Task ComicAsync()
        {
            if (Context.Message.Attachments == null ||
                Context.Message.Attachments.Count <= 0)
            {
                await HandleErrorAsync(ComicError.NoAttachment);
                return;
            }

            var attachment = Context.Message.Attachments.First();
            await Context.Channel.SendMessageAsync($"Your file name is: {Context.Message.Attachments.First().Filename}");

            var response = await new HttpClient().GetAsync(attachment.Url);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine("An error occurred when getting the file.");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var strings = Regex.Split(content, @"[\r\n]")
                .Where(str => !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str));

            if (strings.Count() > 5)
            {
                await HandleErrorAsync(ComicError.TooManyImages);
                return;
            }

            var imageList = new List<object>();

            foreach (var str in strings)
            {
                if (!_specializedDialogRegex.IsMatch(str) &&
                    !_dialogRegex.IsMatch(str))
                {
                    await HandleErrorAsync(ComicError.InvalidCommand);
                    return;
                }

                if (_specializedDialogRegex.IsMatch(str))
                {
                    var match = _specializedDialogRegex.Match(str);

                    if (!_availableSpecializations.ContainsKey(match.Groups[1].Value))
                    {
                        await HandleErrorAsync(ComicError.NoSuchSpecialization);
                        return;
                    }

                    var character = _availableSpecializations[match.Groups[1].Value];

                    var specializationInfo = await DialogService
                        .GetSpecializationInformation(character);

                    var background = match.Groups[2].Value;

                    if (!PersistenceService.DialogBackgrounds.Contains(background))
                    {
                        await HandleErrorAsync(ComicError.NoSuchBackground);
                        return;
                    }

                    var pose = int.Parse(match.Groups[3].Value);

                    if (!specializationInfo.ContainsKey(pose))
                    {
                        await HandleErrorAsync(ComicError.NoSuchPose);
                        return;
                    }

                    var cloth = match.Groups[4].Value;

                    if (!specializationInfo[pose]["Clothes"].Contains(cloth))
                    {
                        await HandleErrorAsync(ComicError.NoSuchCloth);
                        return;
                    }

                    var face = match.Groups[5].Value;

                    if (!specializationInfo[pose]["Faces"].Contains(face))
                    {
                        await HandleErrorAsync(ComicError.NoSuchFace);
                        return;
                    }

                    var text = match.Groups[6].Value.Trim();

                    if (string.IsNullOrEmpty(text) ||
                        string.IsNullOrWhiteSpace(text) ||
                        text.Length <= 0)
                    {
                        await HandleErrorAsync(ComicError.MessageTooShort);
                        return;
                    }

                    if ((DialogService.JapaneseRegex.IsMatch(text) && text.Length > 78) ||
                        text.Length > 120)
                    {
                        await HandleErrorAsync(ComicError.MessageTooLong);
                        return;
                    }

                    if (DialogService.EmojiRegex.IsMatch(text) ||
                        DialogService.EmoteMentionsRegex.IsMatch(text) ||
                        DialogService.NonASCIIAndJapaneseRegex.IsMatch(text))
                    {
                        await HandleErrorAsync(ComicError.WrongCharacterSet);
                        return;
                    }

                    imageList.Add(new
                    {
                        Background = background.ToLower().Trim(),
                        Character = character,
                        Clothes = cloth.ToLower().Trim(),
                        Face = face.ToLower().Trim(),
                        IsHiddenCharacter = (match.Groups[1].Value == "mhirosay") ? true : false,
                        Pose = pose,
                        Text = text
                    });

                }
                else if (_dialogRegex.IsMatch(str))
                {
                    var match = _dialogRegex.Match(str);

                    if (match.Groups[1].Value.ToLower().Trim() != "dialog")
                    {
                        await HandleErrorAsync(ComicError.InvalidCommand);
                        return;
                    }

                    var background = match.Groups[2].Value;

                    if (!PersistenceService.DialogBackgrounds.Contains(background))
                    {
                        await HandleErrorAsync(ComicError.NoSuchBackground);
                        return;
                    }

                    var character = match.Groups[3].Value;

                    if (!PersistenceService.DialogCharacters.Contains(character))
                    {
                        await HandleErrorAsync(ComicError.NoSuchCharacter);
                        return;
                    }

                    var text = match.Groups[4].Value.Trim();

                    if (string.IsNullOrEmpty(text) ||
                        string.IsNullOrWhiteSpace(text) ||
                        text.Length <= 0)
                    {
                        await HandleErrorAsync(ComicError.MessageTooShort);
                        return;
                    }

                    if ((DialogService.JapaneseRegex.IsMatch(text) && text.Length > 78) ||
                        text.Length > 120)
                    {
                        await HandleErrorAsync(ComicError.MessageTooLong);
                        return;
                    }

                    if (DialogService.EmojiRegex.IsMatch(text) ||
                        DialogService.EmoteMentionsRegex.IsMatch(text) ||
                        DialogService.NonASCIIAndJapaneseRegex.IsMatch(text))
                    {
                        await HandleErrorAsync(ComicError.WrongCharacterSet);
                        return;
                    }

                    imageList.Add(new DialogObject
                    {
                        Background = background.ToLower().Trim(),
                        Character = character.ToLower().Trim(),
                        Text = text
                    });
                }
                else
                {
                    await HandleErrorAsync(ComicError.InvalidCommand);
                    return;
                }
            }

            var result = await DialogService
                .GetDialogAsync(Context, imageList, "Cooling down...Try again later.");

            await ReplyAsync("Here you go~");
            await Context.Channel.SendFileAsync(result, "result.png");
        }

        public async Task HandleErrorAsync(Enum error)
        {
            var err = (ComicError)error;

            var msg = err switch
            {
                ComicError.MessageTooShort => "One of the commands doesn't have any content.",
                ComicError.InvalidCommand => "One of the commands is incorrect.",
                ComicError.NoSuchSpecialization => "One of the commands contains nonexistent specialization",
                ComicError.NoSuchBackground => "One of the commands contains a background that is not available",
                ComicError.NoSuchPose => "One of the commands contains an invalid pose",
                ComicError.NoSuchCloth => "One of the commands contains an invalid cloth",
                ComicError.NoSuchFace => "One of the commands contains an invalid face",
                ComicError.NoSuchCharacter => "One of the dialog commands contains an invalid character.",
                ComicError.NoAttachment => "The command has to be called with an attachment.",
                ComicError.MessageTooLong => "The maximum length of text is 120 for English/ASCII characters, 78 for Japanese characters.",
                ComicError.WrongCharacterSet => "This command cannot be used with non-English or non-Japanese text.",
                ComicError.TooManyImages => "The maximum number of images allowed is 5.",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
