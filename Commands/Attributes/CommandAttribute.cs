using System;

namespace TaigaBotCS.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class CommandAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Category;
        public readonly string Description;
        public readonly float Cooldown;
        public readonly string[]? Aliases;
        public readonly string Usage;

        public CommandAttribute(string name, string category, string description, string usage,
            string[]? aliases, float cooldown = 3.0f)
        {
            Name = name;
            Category = category;
            Description = description;
            Usage = usage;
            Aliases = (aliases == null) ? null : aliases;
            Cooldown = cooldown;
        }
    }
}
