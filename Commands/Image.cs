// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("image", "util", null, new[] { "img" })]
    public class Image : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        public ImageService ImageService { get; set; }

        private Dictionary<ulong, Dictionary<string, object>> _imageCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        
        private enum ImageError
        {
            LengthTooShort, LengthTooLong, ImageNotFound
        }

#pragma warning disable CS1998
        [Command("image")]
        [Alias("img")]
        public async Task ImageAsync()
        {
            await HandleErrorAsync(ImageError.LengthTooShort);
            await ImageAsync("hamburger");
        }

        [Command("image")]
        [Alias("img")]
        public async Task ImageAsync(string keyword)
        {
            SetMemberConfig(Context.User.Id);
            var imageErrors = _imageCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            try
            {
                var stream = await ImageService.GetImageAsync(Context, keyword, imageErrors["no_result"].ToString());

                if (stream != null)
                {
                    await ReplyAsync(_imageCommandTexts[Context.User.Id]["result"].ToString()
                        .Replace("{keyword}", keyword));
                    await Context.Channel.SendFileAsync(stream, "image.jpg");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                var msg = imageErrors["generic"].ToString()
                    .Replace("{json}", ex.Message);

                await Context.Channel.SendMessageAsync(msg);

                return;
            }
        }

        [Command("image")]
        [Alias("img")]
        public async Task ImageAsync(string keyword, params string[] rest)
            => _ = HandleErrorAsync(ImageError.LengthTooLong);

        public void SetMemberConfig(ulong userId)
        {
            if (_imageCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper.GetLocalization(
                Helper.GetMemberConfig(userId)?.Language);
            _imageCommandTexts[userId] = responseText.texts.image;
        }

        private async Task HandleErrorAsync(ImageError error)
        {
            SetMemberConfig(Context.User.Id);
            var imageErrors = _imageCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                ImageError.LengthTooShort => imageErrors["length_too_short"].ToString(),
                ImageError.LengthTooLong => imageErrors["length_too_long"].ToString(),
                ImageError.ImageNotFound => imageErrors["no_result"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
