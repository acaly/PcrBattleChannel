using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //'轴'! This is the best translation!
    public class Zhou
    {
        public int ZhouID { get; set; }

        [ForeignKey(nameof(Guild))]
        public int GuildID { get; set; }
        public Guild Guild { get; set; }

        [Display(Name = "轴名")]
        public string Name { get; set; }

        [Display(Name = "说明")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [ForeignKey(nameof(Boss))]
        public int BossID { get; set; }
        [Display(Name = "Boss")]
        public Boss Boss { get; set; }

        [ForeignKey(nameof(C1))]
        public int? C1ID { get; set; }
        public Character C1 { get; set; }

        [ForeignKey(nameof(C2))]
        public int? C2ID { get; set; }
        public Character C2 { get; set; }

        [ForeignKey(nameof(C3))]
        public int? C3ID { get; set; }
        public Character C3 { get; set; }

        [ForeignKey(nameof(C4))]
        public int? C4ID { get; set; }
        public Character C4 { get; set; }

        [ForeignKey(nameof(C5))]
        public int? C5ID { get; set; }
        public Character C5 { get; set; }

        public ICollection<ZhouVariant> Variants { get; set; }
    }
}
