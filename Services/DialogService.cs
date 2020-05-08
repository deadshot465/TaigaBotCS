// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Services
{
    public class DialogService
    {
        private readonly HttpClient _http;
        private readonly HttpClientHandler _handler;

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
    }
}
