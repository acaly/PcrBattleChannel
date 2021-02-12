using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class Guild
    {
        public int GuildID { get; set; }

        [Display(Name = "公会名称")]
        public string Name { get; set; }

        [Display(Name = "公会简介")]
        [DisplayFormat(NullDisplayText = "(空)")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [ForeignKey(nameof(Owner))]
        public string OwnerID { get; set; }

        [Display(Name = "会长")]
        public PcrIdentityUser Owner { get; set; }

        [Display(Name = "会员")]
        public ICollection<PcrIdentityUser> Members { get; set; }

        public int BossIndex { get; set; }
        public float BossDamageRatio { get; set; }

        [DataType(DataType.DateTime)]
        [Obsolete("Use memory storage")]
        public DateTime LastZhouUpdate { get; set; }

        [Display(Name = "Yobot API地址")]
        public string YobotAPI { get; set; }

        public float DamageCoefficient { get; set; }
    }
}
