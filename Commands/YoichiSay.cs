using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("yoichisay", "fun", null, new[] { "yoichi" })]
    public class YoichiSay : SpecializedDialogBase
    {
        public YoichiSay()
        {
            var text = Helper.GetLocalization("en").texts.yoichisay;
            var usage = text["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(YoichiSay),
                new Attributes.CommandAttribute("yoichisay", "fun", usage, new[] { "yoichi" }));
        }

        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            _userLocalization = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language).texts.yoichisay;
        }

        [Command("yoichisay")]
        [Alias("yoichi")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("yoichi", help);
        }

        [Command("yoichisay")]
        [Alias("yoichi")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("yoichi", false, background, pose, clothes, face, content);
        }

        [Command("yoichisay")]
        [Alias("yoichi")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("yoichisay")]
        [Alias("yoichi")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }
    }
}
