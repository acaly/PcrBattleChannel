using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //Many-to-many relationship between user and ZhouVariant.
    public class UserZhouVariant
    {
        public int UserZhouVariantID { get; set; }

        [ForeignKey(nameof(User))]
        public string UserID { get; set; }
        public PcrIdentityUser User { get; set; }

        [ForeignKey(nameof(ZhouVariant))]
        public int ZhouVariantID { get; set; }
        public ZhouVariant ZhouVariant { get; set; }
    }
}
