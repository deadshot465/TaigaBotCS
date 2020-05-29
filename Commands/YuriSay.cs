using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("yurisay", "fun", null, new[] { "yuri" })]
    public class YuriSay : SpecializedDialogBase
    {
        public YuriSay() : base()
        {
            var text = Helper.GetLocalization("en").texts.yurisay;
            var usage = text["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(YuriSay),
                new Attributes.CommandAttribute("yurisay", "fun", usage, new[] { "yuri" }));
        }

        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            _userLocalization = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language).texts.yurisay;
        }

        [Command("yurisay")]
        [Alias("yuri")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("yuri", help);
        }

        [Command("yurisay")]
        [Alias("yuri")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("yuri", false, background, pose, clothes, face, content);
        }

        [Command("yurisay")]
        [Alias("yuri")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("yurisay")]
        [Alias("yuri")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }
    }
}
