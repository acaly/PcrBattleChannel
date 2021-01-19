using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public enum CharacterConfigKind
    {
        [Display(Name = "无要求")]
        Default,
        [Display(Name = "星级")]
        Rarity,
        [Display(Name = "等级")]
        Level,
        [Display(Name = "装备")]
        Rank,
        [Display(Name = "专武")]
        WeaponLevel,
        [Display(Name = "其他")]
        Others,
    }

    public class CharacterConfig
    {
        public int CharacterConfigID { get; set; }

        [ForeignKey(nameof(Guild))]
        public int GuildID { get; set; }
        public Guild Guild { get; set; }

        [ForeignKey(nameof(Character))]
        public int CharacterID { get; set; }
        public Character Character { get; set; }

        [Display(Name = "分类")]
        public CharacterConfigKind Kind { get; set; }

        [Display(Name = "名称")]
        public string Name { get; set; }

        [Display(Name = "描述")]
        public string Description { get; set; }
    }
}
