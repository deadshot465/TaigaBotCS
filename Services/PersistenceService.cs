using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TaigaBotCS.Utility;
using Utf8Json;

namespace TaigaBotCS.Services
{
    public static class PersistenceService
    {
        public static List<string> DialogBackgrounds { get; private set; }
        public static List<string> DialogCharacters { get; private set; }
        public static List<CharacterObject> Routes { get; private set; }
        public static List<CharacterObject> Valentines { get; private set; }

        private static SortedDictionary<ulong, SortedDictionary<string, SortedDictionary<string, dynamic>>> _userStates
            = new SortedDictionary<ulong, SortedDictionary<string, SortedDictionary<string, dynamic>>>();

        private static Dictionary<string, object> _savedData
            = new Dictionary<string, object>();

        private const string _userRecordPath = "./storage/userRecords.json";
        private const string _routePath = "./storage/routes.json";
        private const string _valentinePath = "./storage/valentines.json";

        public static async Task Initialize()
        {
            var routeJson = await File.ReadAllBytesAsync(_routePath);
            Routes = JsonSerializer.Deserialize<List<CharacterObject>>(routeJson);

            var valentineJson = await File.ReadAllBytesAsync(_valentinePath);
            Valentines = JsonSerializer.Deserialize<List<CharacterObject>>(valentineJson);

            if (File.Exists(_userRecordPath))
            {
                var userRecordJson = await File.ReadAllBytesAsync(_userRecordPath);
                _userStates = JsonSerializer
                    .Deserialize<SortedDictionary<ulong, SortedDictionary<string, SortedDictionary<string, dynamic>>>>(userRecordJson);
            }
        }

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

        public static void AddUserRecord(string commandName, string target, ulong userId, params string[] extraInfo)
        {
            if (!_userStates.ContainsKey(userId))
                _userStates.Add(userId, new SortedDictionary<string, SortedDictionary<string, dynamic>>());

            if (!_userStates[userId].ContainsKey(commandName))
                _userStates[userId].Add(commandName, new SortedDictionary<string, dynamic>());

            if (extraInfo.Length > 0)
            {
                if (!_userStates[userId][commandName].ContainsKey(target))
                {
                    _userStates[userId][commandName].Add(target, new SortedDictionary<string, uint>());
                }

                if (!_userStates[userId][commandName][target].ContainsKey(extraInfo[0]))
                {
                    _userStates[userId][commandName][target].Add(extraInfo[0], 1);
                    return;
                }

                _userStates[userId][commandName][target][extraInfo[0]]++;
            }
            else
            {
                if (!_userStates[userId][commandName].ContainsKey(target))
                {
                    _userStates[userId][commandName].Add(target, 1);
                    return;
                }

                _userStates[userId][commandName][target]++;
            }
        }

        public static SortedDictionary<string, dynamic> GetUserRecord(string commandName, ulong userId)
        {
            if (!_userStates.ContainsKey(userId))
                _userStates.Add(userId, new SortedDictionary<string, SortedDictionary<string, dynamic>>());

            if (!_userStates[userId].ContainsKey(commandName))
                _userStates[userId].Add(commandName, new SortedDictionary<string, dynamic>());

            return _userStates[userId][commandName];
        }

        public static SortedDictionary<string, SortedDictionary<string, dynamic>> GetUserRecord(ulong userId)
        {
            if (!_userStates.ContainsKey(userId))
                _userStates.Add(userId, new SortedDictionary<string, SortedDictionary<string, dynamic>>());

            return _userStates[userId];
        }

        public static async Task WriteUserRecord()
            => await File.WriteAllBytesAsync(_userRecordPath, JsonSerializer.Serialize(_userStates));

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
