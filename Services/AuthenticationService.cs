using DotNetEnv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public class AuthenticationService
    {
        public struct AuthResponse
        {
            public string token { get; set; }
            public Dictionary<string, object> userDetails { get; set; }
            public DateTime expiry { get; set; }
        }

        public AuthResponse AuthenticationData { get; private set; }

        private HttpClient _http;

        public AuthenticationService(HttpClient http)
        {
            _http = http;
        }

        public async Task Login()
        {
            var serializedContent = Utf8Json.JsonSerializer.ToJsonString(new
            {
                UserName = Env.GetString("LOGIN_NAME"),
                Password = Env.GetString("LOGIN_PASS"),
            });

            var request = new HttpRequestMessage
            {
                Content = new StringContent(serializedContent, Encoding.UTF8, MediaTypeNames.Application.Json),
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://tetsukizone.com/api/login"),
                Headers =
                {
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                    { HttpRequestHeader.Accept.ToString(), "application/json" }
                }
            };

            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"An error occurred during authentication: {errorMsg}");
                return;
            }

            var responseContent = await response.Content.ReadAsStreamAsync();
            AuthenticationData = await Utf8Json.JsonSerializer
                .DeserializeAsync<AuthResponse>(responseContent);
        }
    }
}
