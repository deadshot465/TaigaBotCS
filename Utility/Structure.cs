using Discord.WebSocket;

namespace TaigaBotCS.Utility
{
    public struct MemberConfig
    {
        public SocketUser User;
        public ulong UserID;
        public string Language;

        public MemberConfig(SocketUser user, ulong userId, string language)
        {
            User = user;
            UserID = userId;
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
        public object cvt;
        public object dialog;
        public object enlarge;
        public object help;
        public object image;
        public object info;
        public object oracle;
        public object ping;
        public object route;
        public object valentine;
        public object ship;
    }

    public struct LocalizationObject
    {
        public string lang;
        public GeneralTextObject texts;
    }
}
