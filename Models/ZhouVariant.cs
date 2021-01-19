using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class ZhouVariant
    {
        public int ZhouVariantID { get; set; }

        [ForeignKey(nameof(Zhou))]
        public int ZhouID { get; set; }
        public Zhou Zhou { get; set; }

        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; }

        public bool IsDraft { get; set; }
        public int Damage { get; set; }

        public ICollection<ZhouVariantCharacterConfig> CharacterConfigs { get; set; }
    }
}
