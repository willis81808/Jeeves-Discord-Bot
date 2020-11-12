using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestDiscordBot.Utils
{
    public static class ExtensionMethods
    {
        public static Color Color(this Random rng)
        {
            return new Color(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255));
        }
    }
}
