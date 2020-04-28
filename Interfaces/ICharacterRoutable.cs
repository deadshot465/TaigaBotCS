// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using Discord;
using System;

namespace TaigaBotCS.Interfaces
{
    public interface ICharacterRoutable
    {
        public string GetFirstName(string name);
        public Tuple<Embed, string> GetEmbeddedMessage();
        public string GetEmoteUrl(string emoteId);
    }
}
