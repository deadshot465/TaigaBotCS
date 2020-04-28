// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utf8Json;

namespace TaigaBotCS.Utility
{
    static class Helper
    {
        private static readonly string CONFIG_PATH = "./storage/memberConfigs.json";
        private static readonly string LOCALIZED_STRINGPATH = "./storage/localizedStrings.json";
        private static List<MemberConfig> _memberConfigs = new List<MemberConfig>();
        private static List<LocalizationObject> _localizationObjects = new List<LocalizationObject>();

        public static MemberConfig AddMemberConfig(SocketUser user,
            ulong userId, string language)
        {
            _memberConfigs.Add(new MemberConfig(user, userId, language));
            return _memberConfigs.Last();
        }

        public static LocalizationObject GetLocalization(string langCode)
            => _localizationObjects.Find(obj => obj.lang == langCode);

        public static MemberConfig? GetMemberConfig(ulong userId)
        {
            if (_memberConfigs.Exists(config => config.UserID == userId))
                return _memberConfigs.Find(config => config.UserID == userId);
            else
                return null;
        }

        public static void LoadLocalizedStrings()
        {
            var bytes = File.ReadAllBytes(LOCALIZED_STRINGPATH);
            _localizationObjects = JsonSerializer.Deserialize<List<LocalizationObject>>(bytes);
        }

        public static void ReadMemberConfigs()
        {
            if (File.Exists(CONFIG_PATH))
            {
                var bytes = File.ReadAllBytes(CONFIG_PATH);
                _memberConfigs = JsonSerializer.Deserialize<List<MemberConfig>>(bytes);
                Console.WriteLine("Member configs read successfully.");
            }
            else
            {
                Console.WriteLine("Member configs not found.");
            }
        }

        public static Task WriteMemberConfigs()
        {
            var json = JsonSerializer.Serialize(_memberConfigs);
            return File.WriteAllBytesAsync(CONFIG_PATH, json);
        }
    }
}
