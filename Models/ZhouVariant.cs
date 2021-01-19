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

        [Display(Name = "配置名")]
        public string Name { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "文字轴")]
        public string Content { get; set; }

        [Display(Name = "发布为草稿")]
        public bool IsDraft { get; set; }

        [Display(Name = "平均伤害")]
        public int Damage { get; set; }

        public ICollection<ZhouVariantCharacterConfig> CharacterConfigs { get; set; }
    }
}
