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
using System.Threading.Tasks;
using TaigaBotCS.Utility;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public class DialogService
    {
        private readonly HttpClient _http;
        private readonly HttpClientHandler _handler;

        private Dictionary<string, Dictionary<int, Dictionary<string, List<string>>>> _specializationInfo
            = new Dictionary<string, Dictionary<int, Dictionary<string, List<string>>>>();

        public DialogService(HttpClient http)
        {
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback
                = (sender, cert, chain, sslPolicyErrors) => true;

            _http = new HttpClient(_handler);
        }

        public async Task<Stream> GetDialogAsync(ICommandContext context,
            DialogObject obj, string cooldownMessage)
        {
            var str = Utf8Json.JsonSerializer.ToJsonString(obj);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://tetsukizone.com/api/dialog"),
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }
                },
                Content = new StringContent(str, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await context.Channel.SendMessageAsync(cooldownMessage);
                return null;
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<Stream> GetDialogAsync(ICommandContext context, string character,
            SpecializedDialogObject obj, string cooldownMessage)
        {
            var str = Utf8Json.JsonSerializer.ToJsonString(obj);

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://tetsukizone.com/api/dialog/{character}"),
                Method = HttpMethod.Post,
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }
                },
                Content = new StringContent(str, Encoding.UTF8, "application/json")
            };

            var response = await _http.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await context.Channel.SendMessageAsync(cooldownMessage);
                return null;
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
