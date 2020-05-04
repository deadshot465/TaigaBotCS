// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("about", "info", null, new string[] { "credits", "bot" })]
    public class About : ModuleBase<SocketCommandContext>
    {
        [Command("about")]
        [Alias("credits", "bot")]
        public async Task AboutAsync(params string[] discard)
        {
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    IconUrl = "https://cdn.discordapp.com/emojis/593518771554091011.png",
                    Name = "Taiga from Camp Buddy",
                    Url = "https://blitsgames.com"
                },
                Color = new Color(0xe81615),
                Description = "Taiga was based on the amazing Yuuto, which was made and developed by the community, for the community. \n" +
                "It was inspired by dunste123#0129's Hiro. \n" +
                "Join Yuuto's dev team and start developing on the [project website](http://iamdeja.github.io/yuuto-docs/). \n\n" +
                "Yuuto version 1.0 was made and developed by: \n" +
                "**Arch#0226**, **Dé-Jà-Vu#1004**, **dunste123#0129**, **zsotroav#8941** \n" +
                "Taiga version 2.0 Beta and Yuuto's C# version ported by: \n**Chehui Chou#1250** \n" +
                "Japanese oracle co-translated with: \n**Kirito#9286** \n" +
                "Taiga reactions and feedback shared by: \n" +
                "**Kirito#9286**, **Kachiryoku#0387**, and countless Camp Buddy fans. \n",
                Footer = new EmbedFooterBuilder
                {
                    Text = "Taiga Bot: Release 2.0 Beta (Based on Yuuto Bot Release 1.0) | 2020-04-29"
                }
            };

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
