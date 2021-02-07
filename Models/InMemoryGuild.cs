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

        private readonly InMemoryUser[] _members = new InMemoryUser[30];
        private readonly Dictionary<string, int> _memberIndexMap = new();

        private readonly List<InMemoryZhouVariant> _zhouVariants = new();
        private readonly Dictionary<int, int> _zhouVariantIndexMap = new();
        private readonly Stack<int> _freeZhouIndex = new();

        public DateTime LastZhouUpdate { get; private set; }

        //If cloneFrom is null, no ZhouVariant is matched. Caller should call InMemoryUser.MatchAllZhou.
        public void AddUser(string id, int attemptsToday, string cloneFrom)
        {
            var index = Array.FindIndex(_members, m => m is null);
            var user = new InMemoryUser
            {
                Guild = this,
                UserID = id,
                Index = index,
            };
            user.ClearComboList(3 - attemptsToday);
            _members[index] = user;
            _memberIndexMap[id] = index;
            if (cloneFrom is not null)
            {
                var cloneFromIndex = _memberIndexMap[cloneFrom];
                foreach (var zv in _zhouVariants)
                {
                    zv.UserData[index] = zv.UserData[cloneFromIndex];
                }
            }
        }

        public void DeleteUser(string id)
        {
            if (!_memberIndexMap.Remove(id, out var index))
            {
                throw new KeyNotFoundException();
            }
            _members[index] = null;
            foreach (var zv in _zhouVariants)
            {
                zv.UserData[index] = default;
            }
        }

        private int AddZhouVariantInternal(int id)
        {
            if (_freeZhouIndex.TryPop(out var index))
            {
                _zhouVariantIndexMap[id] = index;
                return index;
            }
            else
            {
                var ret = _zhouVariants.Count;
                _zhouVariants.Add(null);
                _zhouVariantIndexMap[id] = ret;
                return ret;
            }
        }

        private void DeleteZhouVariantInternal(int index)
        {
            foreach (var u in _members)
            {
                u?.RemoveZhouVariant(index);
            }

            var toRemove = _zhouVariants[index];
            _freeZhouIndex.Push(index);
            _zhouVariants[index] = null;
            _zhouVariantIndexMap.Remove(toRemove.ZhouVariantID);
        }

        public void AddZhouVariant(List<UserCharacterConfig> allUserConfigs,
            ZhouVariant variant, Zhou zhou, IEnumerable<ZhouVariantCharacterConfig> configs)
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
            availableUsers.UnionWith(_members.Where(m => m is not null).Select(m => m.UserID));

            //Check characters (default config).
            IEnumerable<string> FilterCharacter(int? characterID)
            {
                if (!characterID.HasValue) return Enumerable.Empty<string>();
                if (!DefaultCharacterIDs.TryGetValue(characterID.Value, out var defaultConfigID))
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

            var userData = new InMemoryUserZhouVariantData[30];
            for (int i = 0; i < 30; ++i)
            {
                var userID = _members[i]?.UserID;
                if (userID is null) continue;
                if (availableUsers.Contains(userID))
                {
                    userData[i].BorrowPlusOne = 6;
                }
                else if (userBorrow.TryGetValue(userID, out var borrow))
                {
                    userData[i].BorrowPlusOne = (byte)(borrow + 1);
                }
            }

            var cclist = variant.CharacterConfigs
                .GroupBy(cc => (character: cc.CharacterIndex, group: cc.OrGroupIndex))
                .Select(g => (key: g.Key.character, list: g.Select(cc => cc.CharacterConfigID ?? 0).ToImmutableArray()));
            var ret = new InMemoryZhouVariant
            {
                Owner = this,
                Index = AddZhouVariantInternal(variant.ZhouVariantID),
                ZhouID = zhou.ZhouID,
                ZhouVariantID = variant.ZhouVariantID,
                BossID = zhou.BossID,
                Damage = variant.Damage,
                UserData = userData,
                CharacterIDs = ImmutableArray.Create(zhou.C1ID ?? 0, zhou.C2ID ?? 0, zhou.C3ID ?? 0, zhou.C4ID ?? 0, zhou.C5ID ?? 0),
                CharacterConfigIDs = Enumerable.Range(0, 4)
                    .Select(ii => cclist.Where(g => g.key == ii).Select(g => g.list).ToImmutableArray())
                    .ToImmutableArray(),
            };
            _zhouVariants[ret.Index] = ret;
            LastZhouUpdate = TimeZoneHelper.BeijingNow;
        }

        public void DeleteZhouVariant(int zvid)
        {
            if (_zhouVariantIndexMap.TryGetValue(zvid, out var index))
            {
                DeleteZhouVariantInternal(index);
                LastZhouUpdate = TimeZoneHelper.BeijingNow;
            }
        }

        public void DeleteZhou(int zid)
        {
            bool changed = false;
            for (int i = _zhouVariants.Count - 1; i >= 0; --i)
            {
                if (_zhouVariants[i].ZhouID == zid)
                {
                    DeleteZhouVariantInternal(i);
                    changed = true;
                }
            }
            if (changed)
            {
                LastZhouUpdate = TimeZoneHelper.BeijingNow;
            }
        }

        //Update damage and character configs. Other fields will not be modified.
        //Note that user selection (borrow index) will also remain unchanged.
        public void UpdateZhouVariant(ZhouVariant dbzv)
        {
            if (!_zhouVariantIndexMap.TryGetValue(dbzv.ZhouVariantID, out var index))
            {
                throw new Exception("Zhou variant not found");
            }
            var zv = _zhouVariants[index];

            var cclist = dbzv.CharacterConfigs
                .GroupBy(cc => (character: cc.CharacterIndex, group: cc.OrGroupIndex))
                .Select(g => (key: g.Key.character, list: g.Select(cc => cc.CharacterConfigID ?? 0).ToImmutableArray()));
            _zhouVariants[index] = new InMemoryZhouVariant
            {
                ZhouID = zv.ZhouID,
                ZhouVariantID = zv.ZhouVariantID,
                Owner = zv.Owner,
                BossID = zv.BossID,
                Damage = dbzv.Damage,
                UserData = zv.UserData,
                CharacterIDs = zv.CharacterIDs,
                CharacterConfigIDs = Enumerable.Range(0, 4)
                    .Select(ii => cclist.Where(g => g.key == ii).Select(g => g.list).ToImmutableArray())
                    .ToImmutableArray(),
            };
            LastZhouUpdate = TimeZoneHelper.BeijingNow;
        }

        public bool TryGetUserById(string id, out InMemoryUser result)
        {
            if (!_memberIndexMap.TryGetValue(id, out var index))
            {
                result = default;
                return false;
            }
            result = _members[index];
            return true;
        }

        public bool TryGetZhouVariantById(int zvid, out InMemoryZhouVariant result)
        {
            if (!_zhouVariantIndexMap.TryGetValue(zvid, out var index))
            {
                result = default;
                return false;
            }
            result = _zhouVariants[index];
            return true;
        }

        public InMemoryZhouVariant GetZhouVariantByIndex(int index)
        {
            return _zhouVariants[index];
        }

        public IEnumerable<InMemoryZhouVariant> ZhouVariants
        {
            get => _zhouVariants.Where(zv => zv is not null);
        }
    }
}
