using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class Character
    {
        public int CharacterID { get; set; }

        [Display(Name = "游戏ID")]
        public int InternalID { get; set; }
        [Display(Name = "角色名")]
        public string Name { get; set; }
        [Display(Name = "初始星级")]
        public int Rarity { get; set; }
        [Display(Name = "专武")]
        public bool HasWeapon { get; set; }
    }
}
