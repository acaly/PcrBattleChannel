using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class UserCharacterStatus
    {
        public int UserCharacterStatusID { get; set; }

        [ForeignKey(nameof(User))]
        public string UserID { get; set; }
        public PcrIdentityUser User { get; set; }

        [ForeignKey(nameof(Character))]
        public int CharacterID { get; set; }
        public Character Character { get; set; }

        //TODO remove this. We only record used characters (many codes assume this).
        public bool IsUsed { get; set; }
    }
}
