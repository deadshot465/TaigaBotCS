using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TaigaBotCS.Services
{
    public class MealService
    {
        private readonly HttpClient _http;

        public MealService(HttpClient http)
            => _http = http;

        public async Task<string> GetMealAsync()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://www.themealdb.com/api/json/v1/1/random.php")
            };

            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsStringAsync();
            else
                return null;
        }
    }
}
