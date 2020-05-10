// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("pick", "util", null, new string[] { "choose" })]
    public class Pick : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        private Dictionary<ulong, Dictionary<string, object>> _pickCommandTexts
            = new Dictionary<ulong, Dictionary<string, object>>();
        private Random _rng = new Random();

        private enum PickError
        {
            LengthTooShort, TimesTooBig
        }

#pragma warning disable CS1998
        [Command("pick")]
        [Alias("choose")]
        public async Task PickAsync()
            => _ = HandleErrorAsync(PickError.LengthTooShort);

        [Command("pick")]
        [Alias("choose")]
        public async Task PickAsync(string times, [Remainder] string options)
        {
            SetMemberConfig(Context.User.Id);

            var optionList = options.Split('|')
                .Select(str => str.Trim())
                .Where(str => !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
                .ToList();

            var isMultiple = false;
            var pickTimes = 0ul;
            if (times.EndsWith("times"))
            {
                var index = times.LastIndexOf('t');
                times = times.Substring(0, index);
                var result = ulong.TryParse(times, out pickTimes);
                isMultiple = true;
                
                if (!result || pickTimes > uint.MaxValue)
                {
                    await HandleErrorAsync(PickError.TimesTooBig);
                    return;
                }
            }
            else
            {
                optionList.Insert(0, times);
            }
            
            if (optionList.Count <= 0)
            {
                await HandleErrorAsync(PickError.LengthTooShort);
                return;
            }

            var msg = string.Empty;

            if (isMultiple)
            {
                var dict = new Dictionary<string, uint>();
                foreach (var option in optionList)
                {
                    dict.Add(option, 0);
                }

                if (pickTimes > 10000000)
                {
                    Thread[] workerThreads = new Thread[5];
                    var startIndex = 0u;
                    var endIndex = (uint)pickTimes / (uint)workerThreads.Length;
                    var range = endIndex - startIndex;
                    for (var i = 0; i < workerThreads.Length; i++)
                    {
                        workerThreads[i] = new Thread(_ => HandlePick(optionList, ref dict, startIndex, endIndex));
                        startIndex = endIndex + 1u;
                        endIndex = (endIndex + range > (uint)pickTimes) ? (uint)pickTimes : (endIndex + range);
                    }

                    foreach (var worker in workerThreads)
                    {
                        worker.Start();
                    }

                    foreach (var worker in workerThreads)
                    {
                        worker.Join();
                    }
                }
                else
                {
                    for (uint i = 0; i < pickTimes; i++)
                    {
                        dict[optionList[_rng.Next(0, optionList.Count)]]++;
                    }
                }

                var orderedDict = dict.OrderByDescending(pair => pair.Value);

                var builder = new StringBuilder();
                builder.Append(_pickCommandTexts[Context.User.Id]["result"].ToString()
                    .Replace("{option}", orderedDict.First().Key));
                builder.Append("\nTotal times:\n");

                foreach (var option in orderedDict)
                {
                    builder.Append($"{option.Key} - {option.Value} times\n");
                }

                msg = builder.ToString();
            }
            else
            {
                msg = _pickCommandTexts[Context.User.Id]["result"].ToString()
                    .Replace("{option}", optionList[_rng.Next(0, optionList.Count)]);
            }
            
            await ReplyAsync(msg);
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_pickCommandTexts.ContainsKey(userId)) return;

            var responseText = Helper.
                GetLocalization(Helper.GetMemberConfig(userId)?.Language);
            _pickCommandTexts[userId] = responseText.texts.pick;
        }

        private async Task HandleErrorAsync(PickError error)
        {
            SetMemberConfig(Context.User.Id);
            var pickErrors = _pickCommandTexts[Context.User.Id]["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                PickError.LengthTooShort => pickErrors["length_too_short"].ToString(),
                PickError.TimesTooBig => pickErrors["times_too_big"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }

        private static void HandlePick(List<string> optionList, ref Dictionary<string, uint> dict, uint startIndex, uint endIndex)
        {
            var rng = new Random();

            for (uint i = startIndex; i < endIndex; i++)
            {
                dict[optionList[rng.Next(0, optionList.Count)]]++;
            }
        }
    }
}
