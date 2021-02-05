using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //default of this struct is used for new user.
    public struct InMemoryUserZhouVariantData
    {
        //0: invalid, 1-5: borrow, 6: no borrow.
        public byte BorrowPlusOne;
    }

    //Track a ZhouVariant entry in database.
    public class InMemoryZhouVariant
    {
        public InMemoryGuild Owner { get; init; }
        public int ZhouVariantID { get; init; }

        public int BossID { get; init; }
        public int Damage { get; init; }

        public InMemoryUserZhouVariantData[] UserData { get; init; }
    }
}
