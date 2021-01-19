using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class PcrIdentityUser : IdentityUser
    {
        public string GameID { get; set; }
        public ulong QQID { get; set; }

        public int? GuildID { get; set; }
        public Guild Guild { get; set; }
        public bool IsGuildAdmin { get; set; }

        public ICollection<UserCharacterStatus> CharacterStatuses { get; set; }
        public int Attempts { get; set; }

        public ICollection<UserZhouVariant> ZhouVariants { get; set; }
        public ICollection<UserCombo> Combos { get; set; }

        public int NextAttemptPlanIndex { get; set; }
    }
}
