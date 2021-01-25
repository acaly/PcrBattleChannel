using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        //TODO some of these navigation collections are not used

        public ICollection<UserZhouVariant> ZhouVariants { get; set; }
        public ICollection<UserCombo> Combos { get; set; }

        public ICollection<UserCharacterStatus> CharacterStatuses { get; set; }
        public int Attempts { get; set; }
        public int GuessedAttempts { get; set; } //Number of attempts that are from guessing.

        //Attempts

        [ForeignKey(nameof(Attempt1))]
        public int? Attempt1ID { get; set; }
        public UserZhouVariant Attempt1 { get; set; }
        public int? Attempt1Borrow { get; set; }

        [ForeignKey(nameof(Attempt2))]
        public int? Attempt2ID { get; set; }
        public UserZhouVariant Attempt2 { get; set; }
        public int? Attempt2Borrow { get; set; }

        [ForeignKey(nameof(Attempt3))]
        public int? Attempt3ID { get; set; }
        public UserZhouVariant Attempt3 { get; set; }
        public int? Attempt3Borrow { get; set; }

        public DateTime? LastComboUpdate { get; set; }
    }
}
