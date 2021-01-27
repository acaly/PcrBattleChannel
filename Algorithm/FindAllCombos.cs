using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    internal static class FindAllCombos
    {
        private class ZVWrapper
        {
            public static readonly ZVWrapper Null = new();

            public UserZhouVariant UV { get; }
            public int? UVID { get; }
            public Dictionary<int, int> CharactersNoBorrow { get; } // CharacterID -> character index in Zhou
            public Dictionary<int, int> Characters { get; }
            public int? ActualBorrowIndex { get; set; }
            public int BossID => UV?.ZhouVariant.Zhou.BossID ?? int.MaxValue;

            private ZVWrapper() { }

            public ZVWrapper(ApplicationDbContext context, UserZhouVariant uv)
            {
                UV = uv;
                UVID = uv.UserZhouVariantID;

                //Load Zhou (to access characters).
                context.Entry(uv.ZhouVariant).Reference(vv => vv.Zhou).Load();

                //Calculate the hash set.
                Characters = new();
                var z = uv.ZhouVariant.Zhou;
                if (z.C1ID.HasValue) Characters.Add(z.C1ID.Value, 0);
                if (z.C2ID.HasValue) Characters.Add(z.C2ID.Value, 1);
                if (z.C3ID.HasValue) Characters.Add(z.C3ID.Value, 2);
                if (z.C4ID.HasValue) Characters.Add(z.C4ID.Value, 3);
                if (z.C5ID.HasValue) Characters.Add(z.C5ID.Value, 4);

                CharactersNoBorrow = new(Characters);
                if (uv.Borrow == 0) CharactersNoBorrow.Remove(z.C1ID.Value);
                if (uv.Borrow == 1) CharactersNoBorrow.Remove(z.C2ID.Value);
                if (uv.Borrow == 2) CharactersNoBorrow.Remove(z.C3ID.Value);
                if (uv.Borrow == 3) CharactersNoBorrow.Remove(z.C4ID.Value);
                if (uv.Borrow == 4) CharactersNoBorrow.Remove(z.C5ID.Value);

                ActualBorrowIndex = uv.Borrow;
            }

            public bool ApplyUsedCharacterList(HashSet<int> usedCharacters, HashSet<int> test)
            {
                test.Clear();
                test.UnionWith(usedCharacters);
                test.IntersectWith(CharactersNoBorrow.Keys);
                if (test.Count == 0)
                {
                    return true;
                }
                if (ActualBorrowIndex.HasValue || test.Count > 1)
                {
                    return false;
                }
                CharactersNoBorrow.Remove(test.First(), out var borrow);
                ActualBorrowIndex = borrow;
                return true;
            }
        }

        public static async Task RunAsync(ApplicationDbContext context, PcrIdentityUser user, List<UserCharacterStatus> overrideStatus,
            List<UserCombo> results, bool inherit)
        {
            HashSet<int> test = new();
            var borrowCalculator = new FindBorrowCases();

            //Collect all variants.
            var allVariants = await context.UserZhouVariants
                .Include(v => v.ZhouVariant)
                .Where(v => v.UserID == user.Id && !v.ZhouVariant.IsDraft)
                .ToListAsync();

            //Get all used characters.
            var allUsedCharacterList = overrideStatus?.Select(s => s.CharacterID).ToList() ?? 
                await context.UserCharacterStatuses
                    .Where(s => s.UserID == user.Id && s.IsUsed == true)
                    .Select(s => s.CharacterID)
                    .ToListAsync();
            var allUsedCharacterSet = allUsedCharacterList.ToHashSet();

            InheritCombo.ComboInheritInfo inheritComboInfo = null;
            if (inherit)
            {
                inheritComboInfo = await InheritCombo.GetInheritInfo(context, user, allUsedCharacterSet);
                if (inheritComboInfo is not null && inheritComboInfo.Count != 3 - user.Attempts)
                {
                    //Number of zhous must be the same.
                    inheritComboInfo = null;
                }
            }

            var wrappedVariants = new List<ZVWrapper>();
            foreach (var v in allVariants.Select(v => new ZVWrapper(context, v)))
            {
                if (v.ApplyUsedCharacterList(allUsedCharacterSet, test))
                {
                    wrappedVariants.Add(v);
                }
            }

            //Merge by Zhou+BorrowIndex, select highest.
            var highestVariants = wrappedVariants
                .GroupBy(v => (v.UV.ZhouVariant.ZhouID, v.ActualBorrowIndex))
                .Select(g => g.OrderByDescending(v => v.UV.ZhouVariant.Damage).First())
                .ToList();

            //Calculate most-used 4 characters.
            int[] mostUsedCharacters;
            {
                Dictionary<int, int> characterCount = new();
                void AddC(int? i)
                {
                    if (!i.HasValue) return;
                    characterCount.TryGetValue(i.Value, out var old); //Give 0 if not found.
                    characterCount[i.Value] = old + 1;
                }
                foreach (var v in highestVariants)
                {
                    AddC(v.UV.ZhouVariant.Zhou.C1ID);
                    AddC(v.UV.ZhouVariant.Zhou.C2ID);
                    AddC(v.UV.ZhouVariant.Zhou.C3ID);
                    AddC(v.UV.ZhouVariant.Zhou.C4ID);
                    AddC(v.UV.ZhouVariant.Zhou.C5ID);
                }
                mostUsedCharacters = characterCount
                    .OrderByDescending(p => p.Value)
                    .Take(4)
                    .Select(p => p.Key)
                    .ToArray();
            }

            //Group variants into special groups depending on their use of most-used characters.
            List<ZVWrapper> groupA = new(), groupB = new(), groupC = new();
            {
                static bool HasOne(ZVWrapper uv, int i)
                {
                    var z = uv.UV.ZhouVariant.Zhou;
                    return z.C1ID == i || z.C2ID == i ||
                        z.C3ID == i || z.C4ID == i || z.C5ID == i;
                }
                bool IsA(ZVWrapper uv)
                {
                    return HasOne(uv, mostUsedCharacters[0]) &&
                        HasOne(uv, mostUsedCharacters[1]) &&
                        HasOne(uv, mostUsedCharacters[2]);
                }
                bool IsB(ZVWrapper uv)
                {
                    return HasOne(uv, mostUsedCharacters[0]) &&
                        HasOne(uv, mostUsedCharacters[1]) &&
                        HasOne(uv, mostUsedCharacters[3]);
                }
                foreach (var uv in highestVariants)
                {
                    if (IsA(uv))
                    {
                        groupA.Add(uv);
                    }
                    else if (IsB(uv))
                    {
                        groupB.Add(uv);
                    }
                    else
                    {
                        groupC.Add(uv);
                    }
                }
            }

            //Helper functions.
            UserCombo inheritedNewCombo = null;
            bool IsCompatible2(ZVWrapper uv1, ZVWrapper uv2)
            {
                test.Clear();
                test.UnionWith(uv1.CharactersNoBorrow.Keys);
                test.UnionWith(uv2.CharactersNoBorrow.Keys);
                return test.Count >= 8;
            }
            bool IsCompatible3(ZVWrapper uv1, ZVWrapper uv2, ZVWrapper uv3)
            {
                test.Clear();
                test.UnionWith(uv1.CharactersNoBorrow.Keys);
                test.UnionWith(uv2.CharactersNoBorrow.Keys);
                test.UnionWith(uv3.CharactersNoBorrow.Keys);
                return test.Count >= 12;
            }
            void Output(ZVWrapper uv1, ZVWrapper uv2, ZVWrapper uv3)
            {
                //Order by BossID.
                if (uv2.BossID < uv1.BossID)
                {
                    (uv1, uv2) = (uv2, uv1);
                }
                if (uv3.BossID < uv2.BossID)
                {
                    (uv2, uv3) = (uv3, uv2);
                }
                if (uv2.BossID < uv1.BossID)
                {
                    (uv1, uv2) = (uv2, uv1);
                }
                var borrow = borrowCalculator.Run(uv1.Characters, uv2.Characters, uv3.Characters,
                        uv1.ActualBorrowIndex, uv2.ActualBorrowIndex, uv3.ActualBorrowIndex);
                var combo = new UserCombo
                {
                    UserID = user.Id,
                    GuildID = user.GuildID.Value,
                    Zhou1ID = uv1.UVID,
                    Zhou2ID = uv2.UVID,
                    Zhou3ID = uv3.UVID,
                    Boss1 = uv1.UV?.ZhouVariant.Zhou.BossID ?? 0,
                    Boss2 = uv2.UV?.ZhouVariant.Zhou.BossID ?? 0,
                    Boss3 = uv3.UV?.ZhouVariant.Zhou.BossID ?? 0,
                    Damage1 = uv1.UV?.ZhouVariant.Damage ?? 0,
                    Damage2 = uv2.UV?.ZhouVariant.Damage ?? 0,
                    Damage3 = uv3.UV?.ZhouVariant.Damage ?? 0,
                    BorrowInfo = borrow,
                };
                if (inheritComboInfo?.Match(uv1.UV?.ZhouVariantID, uv2.UV?.ZhouVariantID, uv3.UV?.ZhouVariantID, ref borrow) ?? false)
                {
                    if (inheritedNewCombo is not null)
                    {
                        //More than one combo. Don't check any more.
                        inheritComboInfo = null;
                    }
                    else
                    {
                        inheritedNewCombo = combo;
                        combo.BorrowInfo = borrow;
                    }
                }
                context.UserCombos.Add(combo);
                results?.Add(combo);
            }

            //Iterate and generate results (3 cases).

            //Combo with 3 Zhous.
            if (user.Attempts == 0)
            {
                //A (1) + B (1) + C (1)
                foreach (var x in groupA)
                {
                    foreach (var y in groupB)
                    {
                        if (!IsCompatible2(x, y)) continue;
                        foreach (var z in groupC)
                        {
                            if (!IsCompatible2(x, z) || !IsCompatible2(y, z)) continue;
                            if (!IsCompatible3(x, y, z)) continue;
                            Output(x, y, z);
                        }
                    }
                }

                //A (1) + C (2)
                foreach (var x in groupA)
                {
                    for (int j = 0; j < groupC.Count; ++j)
                    {
                        var y = groupC[j];
                        if (!IsCompatible2(x, y)) continue;
                        for (int k = j + 1; k < groupC.Count; ++k)
                        {
                            var z = groupC[k];
                            if (!IsCompatible2(x, z) || !IsCompatible2(y, z)) continue;
                            if (!IsCompatible3(x, y, z)) continue;
                            Output(x, y, z);
                        }
                    }
                }

                //B (1) + C (2)
                foreach (var x in groupB)
                {
                    for (int j = 0; j < groupC.Count; ++j)
                    {
                        var y = groupC[j];
                        if (!IsCompatible2(x, y)) continue;
                        for (int k = j + 1; k < groupC.Count; ++k)
                        {
                            var z = groupC[k];
                            if (!IsCompatible2(x, z) || !IsCompatible2(y, z)) continue;
                            if (!IsCompatible3(x, y, z)) continue;
                            Output(x, y, z);
                        }
                    }
                }

                //C (3)
                for (int i = 0; i < groupC.Count; ++i)
                {
                    var x = groupC[i];
                    for (int j = i + 1; j < groupC.Count; ++j)
                    {
                        var y = groupC[j];
                        if (!IsCompatible2(x, y)) continue;
                        for (int k = j + 1; k < groupC.Count; ++k)
                        {
                            var z = groupC[k];
                            if (!IsCompatible2(x, z) || !IsCompatible2(y, z)) continue;
                            if (!IsCompatible3(x, y, z)) continue;
                            Output(x, y, z);
                        }
                    }
                }
            }
            else if (user.Attempts == 1)
            {
                //A (1) + B (1)
                foreach (var x in groupA)
                {
                    foreach (var y in groupB)
                    {
                        if (!IsCompatible2(x, y)) continue;
                        Output(x, y, ZVWrapper.Null);
                    }
                }

                //A (1) + C (1)
                foreach (var x in groupA)
                {
                    foreach (var y in groupC)
                    {
                        if (!IsCompatible2(x, y)) continue;
                        Output(x, y, ZVWrapper.Null);
                    }
                }

                //B (1) + C (1)
                foreach (var x in groupB)
                {
                    foreach (var y in groupC)
                    {
                        if (!IsCompatible2(x, y)) continue;
                        Output(x, y, ZVWrapper.Null);
                    }
                }

                //C (2)
                for (int i = 0; i < groupC.Count; ++i)
                {
                    var x = groupC[i];
                    for (int j = i + 1; j < groupC.Count; ++j)
                    {
                        var y = groupC[j];
                        if (!IsCompatible2(x, y)) continue;
                        Output(x, y, ZVWrapper.Null);
                    }
                }
            }
            else
            {
                foreach (var x in highestVariants)
                {
                    Output(x, ZVWrapper.Null, ZVWrapper.Null);
                }
            }

            inheritComboInfo?.Setup(inheritedNewCombo);

            user.LastComboUpdate = TimeZoneHelper.BeijingNow;
        }
    }
}
