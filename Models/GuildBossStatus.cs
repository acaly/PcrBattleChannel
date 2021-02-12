using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    [Obsolete("Use memory storage")]
    public class GuildBossStatus
    {
        public int GuildBossStatusID { get; set; }

        [ForeignKey(nameof(Guild))]
        public int GuildID { get; set; }
        public Guild Guild { get; set; }

        [ForeignKey(nameof(Boss))]
        public int BossID { get; set; }
        public Boss Boss { get; set; }

        public int DisplayRow { get; set; }
        public int DisplayColumn { get; set; }
        public string Name { get; set; } //TODO delete this

        public float DamageRatio { get; set; }
    }
}
