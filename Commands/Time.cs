using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TaigaBotCS.Interfaces;
using TaigaBotCS.Utility;
using Utf8Json;

namespace TaigaBotCS.Commands
{
    [Attributes.Command("time", "info", null, new[] { "clock" })]
    public class Time : ModuleBase<SocketCommandContext>, IErrorHandler, IMemberConfigurable
    {
        private enum TimeError
        {
            LengthTooShort, NoResult
        }

        private Dictionary<string, object> _userLocalization;
        private HttpClient _http;
        private TextInfo _textInfo;

        public Time() : base()
        {
            _http = new HttpClient();
            _textInfo = new CultureInfo("en-us", false).TextInfo;
        }

        public async Task HandleErrorAsync(Enum error)
        {
            SetMemberConfig(Context.User.Id);
            var err = (TimeError)error;
            var errorText = _userLocalization["errors"] as Dictionary<string, object>;

            var msg = err switch
            {
                TimeError.LengthTooShort => errorText["length_too_short"].ToString(),
                TimeError.NoResult => errorText["no_result"].ToString(),
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(msg))
                await Context.Channel.SendMessageAsync(msg);
        }

        public void SetMemberConfig(ulong userId)
        {
            if (_userLocalization != null) return;
            _userLocalization = Helper
                .GetLocalization(Helper.GetMemberConfig(Context.User.Id)?.Language).texts.time;
        }

        [Command("time")]
        [Alias("clock")]
        [Priority(3)]
        public async Task TimeAsync()
            => await HandleErrorAsync(TimeError.LengthTooShort);

        [Command("time")]
        [Alias("clock")]
        [Priority(9)]
        public async Task TimeAsync([Remainder] string cityName)
        {
            SetMemberConfig(Context.User.Id);
            cityName = _textInfo.ToTitleCase(cityName)
                .Trim()
                .Replace(' ', '_');

            var response = await _http.GetAsync("http://worldtimeapi.org/api/timezone/");
            var stream = await response.Content.ReadAsStreamAsync();
            var timeZoneObjects = await JsonSerializer.DeserializeAsync<List<object>>(stream);
            var timeZoneList = timeZoneObjects.Cast<string>();

            foreach (var timeZone in timeZoneList)
            {
                if (!timeZone.Contains(cityName)) continue;

                response = await _http.GetAsync($"http://worldtimeapi.org/api/timezone/{timeZone}");
                stream = await response.Content.ReadAsStreamAsync();
                var timeZoneObject = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(stream);
                var result = DateTimeOffset.TryParse(timeZoneObject["datetime"].ToString(), out var time);

                if (!result)
                {
                    await Context.Channel.SendMessageAsync("Parsing time failed.");
                    return;
                }

                var msg = _userLocalization["result"].ToString()
                    .Replace("{city}", timeZone.Replace('_', ' '))
                    .Replace("{time}", time.ToString());

                await Context.Channel.SendMessageAsync(msg);
                return;
            }

            await HandleErrorAsync(TimeError.NoResult);
        }
    }
}
