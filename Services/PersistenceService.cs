using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public static class PersistenceService
    {
        public static List<string> DialogBackgrounds { get; private set; }
        public static List<string> DialogCharacters { get; private set; }

        private static Dictionary<string, object> _savedData
            = new Dictionary<string, object>();

        public static async Task LoadDialogData()
        {
            var http = new HttpClient();
            var response = await http.GetAsync("https://tetsukizone.com/api/dialog");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"Failed to load dialog data.\nError Message: {errorMsg}");
                return;
            }

            var data = await response.Content.ReadAsStreamAsync();
            var deserializedData = await JsonSerializer
                .DeserializeAsync<Dictionary<string, object>>(data);

            var characters = (deserializedData["characters"] as List<object>).Cast<string>();
            var backgrounds = (deserializedData["backgrounds"] as List<object>).Cast<string>();

            DialogCharacters = new List<string>(characters);
            DialogBackgrounds = new List<string>(backgrounds);
        }

        public static bool SaveData<T>(string key, T value)
        {
            _savedData[key] = value;
            return true;
        }

        public static T GetSavedData<T>(string key)
        {
            return (T)_savedData[key];
        }
    }
}
