using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //Track a Guild entry in database.
    public class InMemoryGuild
    {
        public InMemoryStorage Storage { get; init; }
        public SemaphoreSlim GuildLock { get; } = new(1);

        public InMemoryUser[] Members { get; } = new InMemoryUser[30];
        public Dictionary<int, int> MemberIndexMap { get; } = new();
        public List<InMemoryZhouVariant> Zhous { get; } = new();
    }
}
