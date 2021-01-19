using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class BattleStage
    {
        public int BattleStageID { get; set; }

        [ForeignKey(nameof(GlobalData))]
        public int GlobalDataID { get; set; }
        public GlobalData GlobalData { get; set; }

        public int Order { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public int StartLap { get; set; }
    }
}
