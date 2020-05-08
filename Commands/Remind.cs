// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Services;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("remind", "util", null, null)]
    public class Remind : ModuleBase<SocketCommandContext>, IMemberConfigurable
    {
        public ReminderService ReminderService { get; set; }

        private readonly string[] _units = new[]
        {
            "years", "months", "days", "hours", "minutes", "seconds"
        };

        private Dictionary<string, object> _remindCommandTexts
            = new Dictionary<string, object>();

        private enum RemindError
        {
            LengthTooShort, NoSuchOperation, PastTime, NoMessage
        }

        public Remind() : base()
        {
            var commandText = Helper
                .GetLocalization("en");

            var usage = commandText.texts.remind["usage"].ToString()
                .Replace("{units}", string.Join(", ", _units));

            TypeDescriptor.AddAttributes(typeof(Remind),
                new Attributes.CommandAttribute("remind", "util", usage, null));
        }

        [Command("remind")]
        [Priority(5)]
        public async Task RemindAsync()
            => await HandleErrorAsync(RemindError.LengthTooShort);

        [Command("remind")]
        [Priority(3)]
        public async Task RemindAsync([Remainder] string discard)
            => await HandleErrorAsync(RemindError.NoMessage);

        [Command("remind")]
        [Priority(10)]
        public async Task RemindAsync(string prep, int amount, string unit, [Remainder] string message)
        {
            SetMemberConfig(Context.User.Id);
            if (prep != "in")
            {
                await HandleErrorAsync(RemindError.NoSuchOperation);
                return;
            }

            var dueTime = unit switch
            {
                string x when x == "years" || x == "yr" => DateTime.Now.AddYears(amount),
                "months" => DateTime.Now.AddMonths(amount),
                "days" => DateTime.Now.AddDays(amount),
                string x when x == "hours" || x == "hr" => DateTime.Now.AddHours(amount),
                string x when x == "minutes" || x == "min" => DateTime.Now.AddMinutes(amount),
                string x when x == "seconds" || x == "sec" => DateTime.Now.AddSeconds(amount),
                _ => DateTime.Now
            };

            if (dueTime <= DateTime.Now)
            {
                await HandleErrorAsync(RemindError.PastTime);
                return;
            }

            var result = ReminderService.SetReminder(Context.User.Id, dueTime, message);
            if (result != null)
            {
                var msg = _remindCommandTexts["result"].ToString()
                    .Replace("{time}", DateTime.Now.ToString())
                    .Replace("{dueTime}", result.ToString());
                await Context.Channel.SendMessageAsync(msg);
            }
        }

        [Command("remind")]
        [Priority(7)]
        public async Task RemindAsync(string prep, DateTime dateTime, [Remainder] string message)
        {
            SetMemberConfig(Context.User.Id);
            if (prep != "on")
            {
                await HandleErrorAsync(RemindError.NoSuchOperation);
                return;
            }

            if (dateTime <= DateTime.Now)
            {
                await HandleErrorAsync(RemindError.PastTime);
                return;
            }

            var result = ReminderService.SetReminder(Context.User.Id, dateTime, message);
            if (result != null)
            {
                var msg = _remindCommandTexts["result"].ToString()
                    .Replace("{time}", DateTime.Now.ToString())
                    .Replace("{dueTime}", result.ToString());
                await Context.Channel.SendMessageAsync(msg);
            }
        }

        public void SetMemberConfig(ulong userId)
        {
            var responseText = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language);
            _remindCommandTexts = responseText.texts.remind;
        }

        private async Task HandleErrorAsync(RemindError error)
        {
            SetMemberConfig(Context.User.Id);
            var remindErrors = _remindCommandTexts["errors"] as Dictionary<string, object>;

            var msg = error switch
            {
                RemindError.LengthTooShort => remindErrors["length_too_short"].ToString(),
                RemindError.NoSuchOperation => remindErrors["no_such_operation"].ToString()
                .Replace("{units}", string.Join(", ", _units)),
                RemindError.PastTime => remindErrors["past_time"].ToString(),
                RemindError.NoMessage => remindErrors["no_message"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }
    }
}
