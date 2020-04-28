// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("ping", "info", null, null)]
    public class Ping : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task PingAsync(params string[] rest)
        {
            var memberConfig = Helper.GetMemberConfig(Context.User.Id);
            var responseText = Helper.GetLocalization(memberConfig?.Language);
            var pingCmd = responseText.texts.ping;
            var pingInfos = pingCmd["infos"] as Dictionary<string, object>;

            var now = DateTime.Now;
            await Context.Channel.SendMessageAsync(pingInfos["pinging"].ToString()).Result.ModifyAsync(property =>
            {
                var response = pingInfos["responding"].ToString()
                .Replace("{latency}", (DateTime.Now - now).TotalMilliseconds.ToString())
                .Replace("{apiLatency}", Context.Client.Latency.ToString());
                property.Content = response;
            });
        }
    }
}
