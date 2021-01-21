using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public class UserCombo
    {
        public int UserComboID { get; set; }

        public int? SelectedZhou { get; set; }

        //Value of this combo if all members do not select any combo.
        //This reflects how favored is a combo among all members.
        public float NetValue { get; set; }

        //Value of this combo if this user does not select any combo.
        //This reflects how favored is this combo for this user.
        //Note that combos only have Value when the user has not yet
        //selected.
        public float Value { get; set; }

        [ForeignKey(nameof(User))]
        public string UserID { get; set; }
        public PcrIdentityUser User { get; set; }

        [ForeignKey(nameof(Guild))]
        public int GuildID { get; set; }
        public Guild Guild { get; set; }

        [ForeignKey(nameof(Zhou1))]
        public int? Zhou1ID { get; set; }
        [Display(Name = "第一刀")]
        public UserZhouVariant Zhou1 { get; set; }

        [ForeignKey(nameof(Zhou2))]
        public int? Zhou2ID { get; set; }
        [Display(Name = "第二刀")]
        public UserZhouVariant Zhou2 { get; set; }

        [ForeignKey(nameof(Zhou3))]
        public int? Zhou3ID { get; set; }
        [Display(Name = "第三刀")]
        public UserZhouVariant Zhou3 { get; set; }
    }
}
