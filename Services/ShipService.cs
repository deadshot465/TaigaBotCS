// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TaigaBotCS.Services
{
    public class ShipService
    {
        private HttpClient _http;

        public ShipService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetShipAsync(string url1, string url2)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.alexflipnote.dev/ship?user={url1}&user2={url2}"),
            };

            var response = await _http.SendAsync(request);
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
