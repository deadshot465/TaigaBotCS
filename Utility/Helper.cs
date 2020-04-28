// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Utf8Json;

namespace TaigaBotCS.Utility
{
    static class Helper
    {
        private static readonly string _configPath = "./storage/memberConfigs.json";
        private static readonly string _localizedStringPath = "./storage/localizedStrings.json";
        private static List<MemberConfig> _memberConfigs = new List<MemberConfig>();
        private static List<LocalizationObject> _localizationObjects = new List<LocalizationObject>();

        // $1 -> ID
        private static Regex _userMentionRegex = new Regex(@"<@!?(\d{17,20})>");
        // $1 -> Username, $2 -> Discriminator
        private static Regex _userTag = new Regex(@"(\S.{0,30}\S)\s*#(\d{4})");
        private static Regex _discordIdRegex = new Regex(@"\d{17,20}");

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
            var bytes = File.ReadAllBytes(_localizedStringPath);
            _localizationObjects = JsonSerializer.Deserialize<List<LocalizationObject>>(bytes);
        }

        public static void ReadMemberConfigs()
        {
            if (File.Exists(_configPath))
            {
                var bytes = File.ReadAllBytes(_configPath);
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
            return File.WriteAllBytesAsync(_configPath, json);
        }

        public static IGuildUser[] SearchUser(SocketGuild guild, string query)
        {
            if (guild == null)
                return null;
            if (string.IsNullOrEmpty(query))
                return null;

            IGuildUser matchedUser = null;

            if (_userMentionRegex.IsMatch(query))
            {
                var userId = _userMentionRegex.Match(query).Groups[0].Value;
                matchedUser = guild.Users
                    .Where(u => u.Id == ulong.Parse(userId))
                    .First();
            }
            else if (_userTag.IsMatch(query))
            {
                var match = _userTag.Match(query);
                var userName = match.Groups[0].Value;
                var userDiscriminator = match.Groups[1].Value;

                matchedUser = guild.Users
                    .Where(u =>
                    {
                        return ((u.Username == userName || u.Nickname == userName) &&
                        u.Discriminator == userDiscriminator);
                    }).First();
            }
            else if (_discordIdRegex.IsMatch(query))
            {
                var userId = _discordIdRegex.Match(query).Groups[0].Value;
                matchedUser = guild.Users
                    .Where(u => u.Id == ulong.Parse(userId)).First();
            }

            if (matchedUser != null)
                return new IGuildUser[] { matchedUser };

            var exactMatch = new List<IGuildUser>();
            var wrongCase = new List<IGuildUser>();
            var startsWith = new List<IGuildUser>();
            var contains = new List<IGuildUser>();
            var lowerQuery = query.ToLower();

            Func<string, string, bool> ignoreCase
                = (string str1, string str2) =>
                {
                    if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                        return false;
                    return str1.ToUpper() == str2.ToUpper();
                };

            foreach (var user in guild.Users)
            {
                if (user.Username == query || user.Nickname == query)
                    exactMatch.Add(user);
                else if ((ignoreCase(user.Username, query) ||
                    ignoreCase(user.Nickname, query)) &&
                    exactMatch.Count <= 0)
                {
                    wrongCase.Add(user);
                }
                else if ((user.Username.ToLower().StartsWith(lowerQuery) ||
                    (!string.IsNullOrEmpty(user.Nickname) && user.Nickname.ToLower().StartsWith(lowerQuery))) &&
                    wrongCase.Count <= 0)
                {
                    startsWith.Add(user);
                }
                else if ((user.Username.ToLower().Contains(lowerQuery) ||
                    (!string.IsNullOrEmpty(user.Nickname) && user.Nickname.ToLower().Contains(lowerQuery))) &&
                    startsWith.Count <= 0)
                {
                    contains.Add(user);
                }
            }

            return exactMatch.Concat(wrongCase)
                    .Concat(startsWith).Concat(contains).ToArray();
        }
    }
}
