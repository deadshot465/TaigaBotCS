// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using System;

namespace TaigaBotCS.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GameAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Description { get; private set; }

        public GameAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
