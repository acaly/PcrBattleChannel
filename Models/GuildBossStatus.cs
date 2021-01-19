using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class GuildBossStatus
    {
        public int GuildBossStatusID { get; set; }

        public bool IsPlan { get; set; }

        [ForeignKey(nameof(Guild))]
        public int GuildID { get; set; }
        public Guild Guild { get; set; }

        [ForeignKey(nameof(Boss))]
        public int BossID { get; set; }
        public Boss Boss { get; set; }

        public int DisplayRow { get; set; }
        public int DisplayColumn { get; set; }
        public string Name { get; set; }

        public float DamageRatio { get; set; }
    }
}
