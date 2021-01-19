using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class GlobalData
    {
        public int GlobalDataID { get; set; }

        [Display(Name = "赛季名称")]
        public string SeasonName { get; set; }

        [Display(Name = "开始时间")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Display(Name = "结束时间")]
        [DataType(DataType.DateTime)]
        public DateTime EndTime { get; set; }
    }
}
