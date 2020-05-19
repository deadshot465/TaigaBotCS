using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("taigasay", "fun", null, new[] { "taiga" })]
    public class TaigaSay : SpecializedDialogBase
    {
        public TaigaSay() : base()
        {
            var taigaSayText = Helper.GetLocalization("en").texts.taigasay;
            var usage = taigaSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(TaigaSay),
                new Attributes.CommandAttribute("taigasay", "fun", usage, new[] { "taiga" }));
        }

        public override void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;

            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);

            _userLocalization = responseText.texts.taigasay;
        }

        [Command("taigasay")]
        [Alias("taiga")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("taigasay")]
        [Alias("taiga")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("taiga", help);
        }

        [Command("taigasay")]
        [Alias("taiga")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }

        [Command("taigasay")]
        [Alias("taiga")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("taiga", false, background, pose, clothes, face, content);
        }
    }
}
