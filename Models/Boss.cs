using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class Boss
    {
        public int BossID { get; set; }

        [ForeignKey(nameof(BattleStage))]
        public int BattleStageID { get; set; }
        public BattleStage BattleStage { get; set; }

        public string Name { get; set; }
        public string ShortName { get; set; }
        public int Life { get; set; }
        public float Score { get; set; }
    }
}
