using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("hirosay", "fun", null, new[] { "hiro" })]
    public class HiroSay : SpecializedDialogBase
    {
        public HiroSay()
        {
            var hiroSayText = Helper.GetLocalization("en").texts.hirosay;
            var usage = hiroSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(HiroSay),
                new Attributes.CommandAttribute("hirosay", "fun", usage, new[] { "hiro" }));
        }


        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);

            _userLocalization = responseText.texts.hirosay;
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("hiro", help);
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }

        [Command("hirosay")]
        [Alias("hiro")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("hiro", false, background, pose, clothes, face, content);
        }
    }
}
