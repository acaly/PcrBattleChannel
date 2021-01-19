using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //Many-to-many relationship between ZhouVariant and CharacterConfig.
    public class ZhouVariantCharacterConfig
    {
        public int ZhouVariantCharacterConfigID { get; set; }

        [ForeignKey(nameof(ZhouVariant))]
        public int ZhouVariantID { get; set; }
        public ZhouVariant ZhouVariant { get; set; }

        [ForeignKey(nameof(CharacterConfig))]
        public int? CharacterConfigID { get; set; }
        public CharacterConfig CharacterConfig { get; set; }

        //0-4 for the 5 characters.
        public int CharacterIndex { get; set; }

        //Configs with same group index will be or'ed together first, and then
        //all groups are and'ed together.
        //This can be automatically decided by CharacterConfig.Kind.
        public int OrGroupIndex { get; set; }

        public bool AllowBorrow { get; set; }
    }
}
