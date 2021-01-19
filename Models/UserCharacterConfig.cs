using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //Many-to-many relationship between PcrIdentityUser and CharacterConfig.
    public class UserCharacterConfig
    {
        public int UserCharacterConfigID { get; set; }

        [ForeignKey(nameof(User))]
        [Required]
        public string UserID { get; set; }
        public PcrIdentityUser User { get; set; }

        [ForeignKey(nameof(CharacterConfig))]
        public int CharacterConfigID { get; set; }
        public CharacterConfig CharacterConfig { get; set; }
    }
}
