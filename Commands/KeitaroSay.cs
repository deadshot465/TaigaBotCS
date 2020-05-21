using Discord.Commands;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("keitarosay", "fun", null, new[] { "keitaro" })]
    public class KeitaroSay : SpecializedDialogBase
    {
        public KeitaroSay() : base()
        {
            var keitaroSayText = Helper.GetLocalization("en").texts.taigasay;
            var usage = keitaroSayText["usage"].ToString()
                .Replace("{backgrounds}", string.Join(", ", _backgroundString));

            TypeDescriptor.AddAttributes(typeof(TaigaSay),
                new Attributes.CommandAttribute("keitarosay", "fun", usage, new[] { "keitaro" }));
        }

        public override void SetMemberConfig(ulong userId)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);

            _userLocalization = responseText.texts.keitarosay;
        }

        [Command("keitarosay")]
        [Alias("keitaro")]
        [Priority(3)]
        public override async Task SpecializedDialogBaseAsync()
        {
            await base.SpecializedDialogBaseAsync();
        }

        [Command("keitarosay")]
        [Alias("keitaro")]
        [Priority(5)]
        public override async Task SpecializedDialogBaseAsync(string help)
        {
            await SpecializedDialogBaseAsync("keitaro", help);
        }

        [Command("keitarosay")]
        [Alias("keitaro")]
        [Priority(7)]
        public override async Task SpecializedDialogBaseAsync(int pose, string clothes, string face, [Remainder] string content)
        {
            await base.SpecializedDialogBaseAsync(pose, clothes, face, content);
        }

        [Command("keitarosay")]
        [Alias("keitaro")]
        [Priority(9)]
        public override async Task SpecializedDialogBaseAsync(string background, int pose, string clothes, string face, [Remainder] string content)
        {
            await SpecializedDialogBaseAsync("keitaro", false, background, pose, clothes, face, content);
        }
    }
}
