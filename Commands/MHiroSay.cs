using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("mhirosay", "fun", null, new[] { "mhiro", "maturehiro", "maturehirosay" })]
    public class MHiroSay : SpecializedDialogBase
    {
        public MHiroSay() : base()
        {
            var hiroSayText = Helper.GetLocalization("en").texts.hirosay;
            var usage = hiroSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(HiroSay),
                new Attributes.CommandAttribute("mhirosay", "fun", usage, new[] { "mhiro", "maturehiro", "maturehirosay" }));
        }

        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);

            _userLocalization = responseText.texts.hirosay;
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("hiro", help);
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }

        [Command("mhirosay")]
        [Alias("mhiro", "maturehiro", "maturehirosay")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("hiro", true, background, pose, clothes, face, content);
        }
    }
}
