// Copyright(C) 2020 Tetsuki Syu
// See Program.cs for the full notice.

using System;
using System.Collections.Generic;
using TaigaBotCS.Utility;

namespace TaigaBotCS.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class CommandAttribute : Attribute
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public float Cooldown { get; private set; }
        public string[] Aliases { get; private set; }

        public string Description { get; set; }
        public string Usage { get; set; }

        /// <summary>
        /// A custom attribute that helps settings up information of a command.
        /// </summary>
        /// <param name="name">The command</param>
        /// <param name="category">The command's category</param>
        /// <param name="usage">(Optional) The command's usage.</param>
        /// <param name="aliases">The command's aliases.</param>
        /// <param name="cooldown">The command's cooldown. Default to 3 seconds.</param>
        public CommandAttribute(string name, string category, string usage, string[] aliases, float cooldown = 3.0f)
        {
            var localizedStrings = Helper.GetLocalization("en");
            var field = localizedStrings.texts.GetType().GetField(name);
            var obj = field?.GetValue(localizedStrings.texts) as Dictionary<string, object>;

            Name = name;
            Category = category;
            Description = obj?["description"].ToString();
            Usage = usage ?? obj?["usage"].ToString();
            Aliases = aliases ?? null;
            Cooldown = cooldown;
        }
    }
}
