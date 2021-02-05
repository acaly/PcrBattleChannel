using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Data
{
    public static class InMemoryStorage
    {
        private static ConcurrentDictionary<int, InMemoryGuild> _guilds = new();

        public static async Task<InMemoryGuild> GetAndLockGuild(ApplicationDbContext context, int guildID)
        {
            var ret = _guilds.GetOrAdd(guildID, CreateFromDb, context);
            await ret.GuildLock.WaitAsync();
            return ret;
        }

        private static InMemoryGuild CreateFromDb(int guildID, ApplicationDbContext dbContext)
        {
            var ret = new InMemoryGuild
            {
                GuildID = guildID,
                DefaultCharacterIDs = dbContext.CharacterConfigs
                    .Where(cc => cc.GuildID == guildID && cc.Kind == CharacterConfigKind.Default)
                    .ToImmutableDictionary(cc => cc.CharacterID, cc => cc.CharacterConfigID)
            };

            var members = dbContext.Users
                .Where(u => u.GuildID == guildID)
                .ToList();
            for (int i = 0; i < members.Count; ++i)
            {
                var member = new InMemoryUser()
                {
                    Guild = ret,
                    UserID = members[i].Id,
                    Index = i,
                    LastComboCalculation = default,
                    ComboZhouCount = 3 - members[i].Attempts,
                };
                ret.Members[i] = member;
                ret.MemberIndexMap.Add(members[i].Id, i);
            }

            var userConfigs = dbContext.UserCharacterConfigs
                .Include(ucc => ucc.User)
                .Where(ucc => ucc.User.GuildID == guildID)
                .ToList();

            var zhouVariants = dbContext.ZhouVariants
                .Include(zv => zv.Zhou)
                .Include(zv => zv.CharacterConfigs)
                .Where(zv => zv.Zhou.GuildID == guildID)
                .ToList();

            foreach (var zv in zhouVariants)
            {
                var conv = CreateUserVariants(ret, ret.DefaultCharacterIDs, userConfigs, zv.Zhou, zv, zv.CharacterConfigs, members);
                ret.Zhous.Add(conv);
            }

            return ret;
        }

        public static InMemoryZhouVariant CreateUserVariants(InMemoryGuild guild,
            ImmutableDictionary<int, int> defaultConfigIDs, List<UserCharacterConfig> allUserConfigs,
            Zhou zhou, ZhouVariant variant, IEnumerable<ZhouVariantCharacterConfig> configs, 
            List<PcrIdentityUser> allUsers)
        {
            var availableUsers = new HashSet<string>();
            var tempSet = new HashSet<string>();
            var userBorrow = new Dictionary<string, int>();
            var deleteList = new List<string>();

            void Merge(IEnumerable<string> list, int borrow)
            {
                HashSet<string> mergeSet;
                if (list is HashSet<string> s)
                {
                    mergeSet = s;
                }
                else
                {
                    tempSet.Clear();
                    tempSet.UnionWith(list);
                    mergeSet = tempSet;
                }

                deleteList.Clear();
                foreach (var (u, b) in userBorrow)
                {
                    if (b != borrow && !mergeSet.Contains(u))
                    {
                        deleteList.Add(u);
                    }
                }
                foreach (var u in deleteList)
                {
                    userBorrow.Remove(u);
                }

                deleteList.Clear();
                foreach (var u in availableUsers)
                {
                    if (!mergeSet.Contains(u))
                    {
                        userBorrow.Add(u, borrow);
                        deleteList.Add(u);
                    }
                }
                foreach (var u in deleteList)
                {
                    availableUsers.Remove(u);
                }
            }

            //Mark all users as available.
            availableUsers.UnionWith(allUsers.Select(u => u.Id));

            //Check characters (default config).
            IEnumerable<string> FilterCharacter(int? characterID)
            {
                if (!characterID.HasValue) return Enumerable.Empty<string>();
                if (!defaultConfigIDs.TryGetValue(characterID.Value, out var defaultConfigID))
                {
                    availableUsers.Clear();
                    return Enumerable.Empty<string>();
                }
                return allUserConfigs
                    .Where(c => c.CharacterConfigID == defaultConfigID)
                    .Select(c => c.UserID);
            }
            Merge(FilterCharacter(zhou.C1ID), 0);
            Merge(FilterCharacter(zhou.C2ID), 1);
            Merge(FilterCharacter(zhou.C3ID), 2);
            Merge(FilterCharacter(zhou.C4ID), 3);
            Merge(FilterCharacter(zhou.C5ID), 4);

            //Check additional configs.
            var orGroupUsers = new HashSet<string>();
            foreach (var configGroup in configs.GroupBy(c => (c.CharacterIndex, c.OrGroupIndex)))
            {
                foreach (var c in configGroup)
                {
                    if (c.CharacterConfigID.HasValue)
                    {
                        orGroupUsers.UnionWith(allUserConfigs
                            .Where(cc => cc.CharacterConfigID == c.CharacterConfigID)
                            .Select(u => u.UserID));
                    }
                    else
                    {
                        orGroupUsers.UnionWith(allUserConfigs
                            .Where(cc => cc.CharacterConfig == c.CharacterConfig)
                            .Select(u => u.UserID));
                    }
                }
                Merge(orGroupUsers, configGroup.Key.CharacterIndex);
            }

            var ret = new InMemoryZhouVariant
            {
                Owner = guild,
                ZhouVariantID = variant.ZhouVariantID,
                BossID = zhou.BossID,
                Damage = variant.Damage,
                UserData = new InMemoryUserZhouVariantData[30],
            };

            for (int i = 0; i < 30; ++i)
            {
                var userID = guild.Members[i]?.UserID;
                if (userID is null) continue;
                if (availableUsers.Contains(userID))
                {
                    ret.UserData[i].BorrowPlusOne = 6;
                }
                else if (userBorrow.TryGetValue(userID, out var borrow))
                {
                    ret.UserData[i].BorrowPlusOne = (byte)(borrow + 1);
                }
            }

            return ret;
        }
    }

    public sealed class InMemoryStorageContext : IDisposable
    {
        public ApplicationDbContext DbContext { get; }
        private readonly Dictionary<int, InMemoryGuild> _guilds = new();

        public InMemoryStorageContext(ApplicationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<InMemoryGuild> GetGuild(int guildID)
        {
            if (!_guilds.TryGetValue(guildID, out var ret))
            {
                ret = await InMemoryStorage.GetAndLockGuild(DbContext, guildID);
                _guilds.Add(guildID, ret);
            }
            return ret;
        }

        public void Dispose()
        {
            foreach (var g in _guilds.Values)
            {
                g.GuildLock.Release();
            }
        }
    }
}
