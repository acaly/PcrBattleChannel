using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public static class StandardConfigNames
    {
        public static readonly (Regex regex, CharacterConfigKind kind)[] Values = new[]
        {
            (new Regex("\\G[123456]星"), CharacterConfigKind.Rarity),
            (new Regex("\\G[Rr]\\d+-[0123456Xx]"), CharacterConfigKind.Rank),
            (new Regex("\\G开专"), CharacterConfigKind.WeaponLevel),
            (new Regex("\\G满专"), CharacterConfigKind.WeaponLevel),
        };

        public static readonly (string regex, string example)[] FrontEndRegexCheck = new[]
        {
            ("a^", ""),
            ("^[123456]星$", "“4星”、“5星”"),
            (".*", ""),
            ("^[R]\\d+-[0123456X]$", "“R9-6”、“R11-3”、“R11-X”"),
            (".*", ""),
            (".*", ""),
        };
    }
}
