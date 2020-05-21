// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TaigaBotCS.Utility;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public class DialogService
    {
        public static readonly Regex EmojiRegex
            = new Regex(@"(?:[\u2700-\u27bf]|(?:\ud83c[\udde6-\uddff]){2}|[\ud800-\udbff][\udc00-\udfff]|[\u0023-\u0039]\ufe0f?\u20e3|\u3299|\u3297|\u303d|\u3030|\u24c2|\ud83c[\udd70-\udd71]|\ud83c[\udd7e-\udd7f]|\ud83c\udd8e|\ud83c[\udd91-\udd9a]|\ud83c[\udde6-\uddff]|\ud83c[\ude01-\ude02]|\ud83c\ude1a|\ud83c\ude2f|\ud83c[\ude32-\ude3a]|\ud83c[\ude50-\ude51]|\u203c|\u2049|[\u25aa-\u25ab]|\u25b6|\u25c0|[\u25fb-\u25fe]|\u00a9|\u00ae|\u2122|\u2139|\ud83c\udc04|[\u2600-\u26FF]|\u2b05|\u2b06|\u2b07|\u2b1b|\u2b1c|\u2b50|\u2b55|\u231a|\u231b|\u2328|\u23cf|[\u23e9-\u23f3]|[\u23f8-\u23fa]|\ud83c\udccf|\u2934|\u2935|[\u2190-\u21ff])");
        public static readonly Regex EmoteMentionsRegex
            = new Regex(@"<(?:[^\d>]+|:[A-Za-z0-9]+:)\w+>");
        public static readonly Regex JapaneseRegex
            = new Regex(@"[\u4e00-\u9fbf\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u3000-\u303f]");
        public static readonly Regex NonASCIIAndJapaneseRegex
            = new Regex(@"[^\x00-\x7F\u4e00-\u9fbf\u3040-\u309f\u30a0-\u30ff\uff00-\uff9f\u3000-\u303f\u2018-\u2019]");

        private readonly HttpClient _http;
        private readonly HttpClientHandler _handler;
        private readonly AuthenticationService _authenticationService;

        private Dictionary<string, Dictionary<int, Dictionary<string, List<string>>>> _specializationInfo
            = new Dictionary<string, Dictionary<int, Dictionary<string, List<string>>>>();

        public DialogService(HttpClient http, AuthenticationService authenticationService)
        {
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback
                = (sender, cert, chain, sslPolicyErrors) => true;

            _http = new HttpClient(_handler);
            _authenticationService = authenticationService;
        }

        public async Task<Stream> GetDialogAsync(ICommandContext context,
            DialogObject obj, string cooldownMessage)
        {
            if (_authenticationService.AuthenticationData.expiry < DateTime.Now)
            {
                await _authenticationService.Login();
            }

            var str = JsonSerializer.ToJsonString(obj);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://tetsukizone.com/api/dialog"),
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_authenticationService.AuthenticationData.token}" }
                },
                Content = new StringContent(str, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await context.Channel.SendMessageAsync(cooldownMessage);
                return null;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _authenticationService.Login();
                return await GetDialogAsync(context, obj, cooldownMessage);
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GetDialogAsync(ICommandContext context, string character,
            SpecializedDialogObject obj, string cooldownMessage)
        {
            if (_authenticationService.AuthenticationData.expiry < DateTime.Now)
            {
                await _authenticationService.Login();
            }

            var str = Utf8Json.JsonSerializer.ToJsonString(obj);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://tetsukizone.com/api/dialog/{character}"),
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_authenticationService.AuthenticationData.token}" }
                },
                Content = new StringContent(str, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await context.Channel.SendMessageAsync(cooldownMessage);
                return null;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _authenticationService.Login();
                return await GetDialogAsync(context, character, obj, cooldownMessage);
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GetDialogAsync(ICommandContext context, IEnumerable<object> dialogs,
            string cooldownMessage)
        {
            if (_authenticationService.AuthenticationData.expiry < DateTime.Now)
            {
                await _authenticationService.Login();
            }

            var str = JsonSerializer.ToJsonString(dialogs);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://tetsukizone.com/api/comic"),
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_authenticationService.AuthenticationData.token}" }
                },
                Content = new StringContent(str, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await context.Channel.SendMessageAsync(cooldownMessage);
                return null;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _authenticationService.Login();
                return await GetDialogAsync(context, dialogs, cooldownMessage);
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Dictionary<int, Dictionary<string, List<string>>>> GetSpecializationInformation(string character)
        {
            if (_specializationInfo != null &&
                _specializationInfo.Count > 0 &&
                _specializationInfo.ContainsKey(character) &&
                _specializationInfo[character].Count > 0)
                return _specializationInfo[character];

            var response = await _http.GetAsync($"https://tetsukizone.com/api/dialog/{character}");

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"An error occurred fetching specialization info: {errorMsg}");
                return null;
            }

            var result = await response.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(result);
            var test = obj["Poses"];
            var poseInfos = obj["Poses"] as Dictionary<string, object>;

            if (!_specializationInfo.ContainsKey(character))
                _specializationInfo.Add(character, new Dictionary<int, Dictionary<string, List<string>>>());

            foreach (var item in poseInfos)
            {
                _specializationInfo[character].Add(int.Parse(item.Key), new Dictionary<string, List<string>>());
                var info = item.Value as Dictionary<string, object>;

                foreach (var _item in info)
                {
                    var list = (_item.Value as List<object>).Cast<string>().OrderBy(x => x);
                    _specializationInfo[character][int.Parse(item.Key)].Add(_item.Key, new List<string>(list));
                }
            }

            return _specializationInfo[character];
        }
    }
}
