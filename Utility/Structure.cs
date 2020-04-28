// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using Discord.WebSocket;
using System.Collections.Generic;

namespace TaigaBotCS.Utility
{
    public struct MemberConfig
    {
        private SocketUser _user;

        public string AvatarID;
        public string AvatarURL;
        public bool IsBot;
        public bool IsWebhook;
        public ulong UserID;
        public string UserName;
        public string Language;

        public MemberConfig(SocketUser user, ulong userId, string language)
        {
            _user = user;

            AvatarID = _user.AvatarId;
            AvatarURL = _user.GetAvatarUrl(ImageFormat.Png, 2048);
            IsBot = _user.IsBot;
            IsWebhook = _user.IsWebhook;
            UserID = userId;
            UserName = _user.Username;
            Language = language;
        }
    }

    public struct GeneralTextObject
    {
        public string[] presence;
        public string[] greetings;
        public string[] random_responses;
        public string[] failed_messages;
        public string cooldown;
        public string execution_failure;
        public Dictionary<string, object> cvt;
        public Dictionary<string, object> dialog;
        public Dictionary<string, object> enlarge;
        public Dictionary<string, object> help;
        public Dictionary<string, object> image;
        public Dictionary<string, object> info;
        public Dictionary<string, object> oracle;
        public Dictionary<string, object> ping;
        public Dictionary<string, object> route;
        public Dictionary<string, object> valentine;
        public Dictionary<string, object> ship;
    }

    public struct LocalizationObject
    {
        public string lang;
        public GeneralTextObject texts;
    }

    public struct DialogObject
    {
        public string background;
        public string character;
        public string text;
    }

    public struct CharacterObject
    {
        public string name;
        public string description;
        public int age;
        public string birthday;
        public string animal;
        public string color;
        public string emoteId;
    }
}
