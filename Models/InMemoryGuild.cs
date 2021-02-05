using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    //Track a Guild entry in database.
    public class InMemoryGuild
    {
        public int GuildID { get; init; }
        public ImmutableDictionary<int, int> DefaultCharacterIDs { get; init; }

        public SemaphoreSlim GuildLock { get; } = new(1);

        public InMemoryUser[] Members { get; } = new InMemoryUser[30];
        public Dictionary<string, int> MemberIndexMap { get; } = new();
        public List<InMemoryZhouVariant> Zhous { get; } = new();

        public void AddUser(string id, string cloneFrom)
        {
            var index = Array.FindIndex(Members, m => m is null);
            var user = new InMemoryUser
            {
                Guild = this,
                UserID = id,
                Index = index,
                LastComboCalculation = default,
                ComboZhouCount = 3,
            };
            Members[index] = user;
            MemberIndexMap[id] = index;
            if (cloneFrom is not null)
            {
                var cloneFromIndex = MemberIndexMap[cloneFrom];
                foreach (var zv in Zhous)
                {
                    zv.UserData[index] = zv.UserData[cloneFromIndex];
                }
            }
        }

        public void DeleteUser(string id)
        {
            if (!MemberIndexMap.Remove(id, out var index))
            {
                throw new KeyNotFoundException();
            }
            Members[index] = null;
            foreach (var zv in Zhous)
            {
                zv.UserData[index] = default;
            }
        }

        public void AddZhouVariant(ZhouVariant dbzv)
        {

        }
    }
}
