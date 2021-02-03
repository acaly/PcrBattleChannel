using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public struct InMemoryUserZhouVariantData
    {
        //0-4: borrow. other values (5): no borrow
        public byte Borrow;
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
