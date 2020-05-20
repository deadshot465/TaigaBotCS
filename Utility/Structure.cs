// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using System.Collections.Generic;

namespace TaigaBotCS.Utility
{
    public class MemberConfig
    {
        public string AvatarID;
        public string AvatarURL;
        public bool IsBot;
        public bool IsWebhook;
        public ulong UserID;
        public string UserName;
        public string Language;

        public MemberConfig(string avatarId, string avatarUrl, bool isBot, bool isWebhook,
            ulong userId, string userName, string language)
        {
            AvatarID = avatarId;
            AvatarURL = avatarUrl;
            IsBot = isBot;
            IsWebhook = isWebhook;
            UserID = userId;
            UserName = userName;
            Language = language;
        }

        public MemberConfig(IUser user, ulong userId, string language)
        {
            AvatarID = user.AvatarId;
            AvatarURL = user.GetAvatarUrl(ImageFormat.Png, 2048);
            IsBot = user.IsBot;
            IsWebhook = user.IsWebhook;
            UserID = userId;
            UserName = user.Username;
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
        public Dictionary<string, object> about;
        public Dictionary<string, object> oracle;
        public Dictionary<string, object> ping;
        public Dictionary<string, object> route;
        public Dictionary<string, object> valentine;
        public Dictionary<string, object> ship;
        public Dictionary<string, object> pick;
        public Dictionary<string, object> meal;
        public Dictionary<string, object> owoify;
        public Dictionary<string, object> remind;
        public Dictionary<string, object> stats;
        public Dictionary<string, object> hirosay;
        public Dictionary<string, object> taigasay;
    }

    public struct LocalizationObject
    {
        public string lang;
        public GeneralTextObject texts;
    }

    public struct DialogObject
    {
        public string Background;
        public string Character;
        public string Text;
    }

    public struct SpecializedDialogObject
    {
        public string Background;
        public string Text;
        public int Pose;
        public string Clothes;
        public string Face;
        public bool IsHiddenCharacter;
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
