// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.Commands;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public class ImageService
    {
        public struct UnsplashUrl
        {
            public string raw;
            public string full;
            public string regular;
            public string small;
            public string thumb;
        }

        public struct UnsplashLink
        {
            public string self;
            public string html;
            public string download;
            public string download_location;
        }

        public struct UnsplashQueryResult
        {
            public string id;
            public string created_at;
            public string updated_at;
            public object promoted_at;
            public int width;
            public int height;
            public string color;
            public string description;
            public string alt_description;
            public UnsplashUrl urls;
            public UnsplashLink links;
            public string[] categories;
            public int likes;
            public bool liked_by_user;
            public string[] current_user_collections;
            public object user;
            public object[] tags;
        }

        public struct UnsplashSearchResult
        {
            public int total;
            public int total_pages;
            public UnsplashQueryResult[] results;
        }

        private const int UNSPLASH_ITEM_PER_PAGE = 10;
        private readonly HttpClient _http;
        private readonly Random _rng = new Random();

        public ImageService(HttpClient http)
            => _http = http;

        public async Task<Stream> GetImageAsync(ICommandContext context, string keyword,
            string noResultErrorMessage)
        {
            var token = DotNetEnv.Env.GetString("UNSPLASH_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://api.unsplash.com/search/photos?client_id={token}&query={keyword}&page=1"),
                    Method = HttpMethod.Get
                };

                var httpResponse = await _http.SendAsync(request);
                var response = await httpResponse.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<UnsplashSearchResult>(response);
                int total = data.total;
                int totalPages = data.total_pages;
                Console.WriteLine($"Total: {total}");

                if (total == 0)
                    await context.Channel.SendMessageAsync(noResultErrorMessage);

                // Limit to the first 25% pages.
                var upperPageLimit = Math.Ceiling(totalPages * 0.25);
                var randomPageNumber = _rng.Next(0, (int)upperPageLimit + 1);

                request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"https://api.unsplash.com/search/photos?client_id={token}&query={keyword}&page={randomPageNumber}"),
                    Method = HttpMethod.Get
                };

                httpResponse = await _http.SendAsync(request);
                response = await httpResponse.Content.ReadAsStringAsync();
                data = JsonSerializer.Deserialize<UnsplashSearchResult>(response);
                var mod = data.total % UNSPLASH_ITEM_PER_PAGE;
                var itemNo = (randomPageNumber == totalPages) ?
                    _rng.Next(0, mod) : _rng.Next(0, UNSPLASH_ITEM_PER_PAGE);
                string link = data.results[itemNo].urls.regular;

                request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{link}"),
                    Method = HttpMethod.Get,
                    Headers =
                    {
                        { HttpRequestHeader.Accept.ToString(), "image/jpeg" },
                        { HttpRequestHeader.ContentType.ToString(), "image/jpeg" }
                    }
                };

                httpResponse = await _http.SendAsync(request);
                return await httpResponse.Content.ReadAsStreamAsync();
            }

            return null;
        }
    }
}
