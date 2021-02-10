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
        private static readonly ConcurrentDictionary<int, InMemoryGuild> _guilds = new();

        public static async Task<InMemoryGuild> GetAndLockGuild(ApplicationDbContext context, int guildID)
        {
            var ret = _guilds.GetOrAdd(guildID, CreateFromDb, context);
            await ret.GuildLock.WaitAsync();
            return ret;
        }

        public static void RemoveGuild(int guildID)
        {
            _guilds.TryRemove(guildID, out _);
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
                ret.AddUser(members[i].Id, members[i].Attempts, null);
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
                ret.AddZhouVariant(userConfigs, zv, zv.Zhou, zv.CharacterConfigs);
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

        public async Task<InMemoryGuild> GetGuildAsync(int guildID)
        {
            if (!_guilds.TryGetValue(guildID, out var ret))
            {
                ret = await InMemoryStorage.GetAndLockGuild(DbContext, guildID);
                _guilds.Add(guildID, ret);
            }
            return ret;
        }

        public async Task RemoveGuildAsync(int guildID)
        {
            await GetGuildAsync(guildID); //Obtain the lock.
            InMemoryStorage.RemoveGuild(guildID);
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
