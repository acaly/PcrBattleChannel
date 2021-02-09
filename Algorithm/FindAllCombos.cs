using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    internal class FindAllCombos
    {
        private class ZVWrapper
        {
            public static readonly ZVWrapper Null = new();

            public InMemoryZhouVariant ZV { get; }
            public Dictionary<int, int> CharactersNoBorrow { get; } //TODO check whether this can be simplified
            public int? ActualBorrowIndex { get; set; }
            public int BossID => ZV?.BossID ?? int.MaxValue;

            private ZVWrapper() { }

            public ZVWrapper(InMemoryUser user, InMemoryZhouVariant zv)
            {
                ZV = zv;

                CharactersNoBorrow = new(ZV.CharacterIndexMap);

                var borrow = zv.UserData[user.Index].BorrowPlusOne - 1;
                if (borrow < 5)
                {
                    CharactersNoBorrow.Remove(zv.CharacterIDs[borrow]);
                    ActualBorrowIndex = borrow;
                }
            }

            //TODO this check should be done before creating this wrapper
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

        private readonly InMemoryComboListBuilder _comboListBuilder = new();

        public void Run(InMemoryUser user, HashSet<int> usedCharacters, int newComboSize,
            InheritCombo.ComboInheritInfo inheritComboInfo, bool includeDraft)
        {
            HashSet<int> test = new();
            var borrowCalculator = new FindBorrowCases();

            var filteredVariants = user.Guild.ZhouVariants
                .Where(zv => (includeDraft || !zv.IsDraft) && zv.UserData[user.Index].BorrowPlusOne != 0)
                .Select(zv => new ZVWrapper(user, zv));
            var wrappedVariants = new List<ZVWrapper>();
            foreach (var v in filteredVariants)
            {
                if (v.ApplyUsedCharacterList(usedCharacters, test))
                {
                    wrappedVariants.Add(v);
                }
            }

            //Merge by Zhou+BorrowIndex, select highest.
            var highestVariants = wrappedVariants
                .GroupBy(v => (v.ZV.ZhouID, v.ActualBorrowIndex))
                .Select(g => g.OrderByDescending(v => v.ZV.Damage).First())
                .ToList();

            //Calculate most-used 4 characters.
            int[] mostUsedCharacters;
            {
                Dictionary<int, int> characterCount = new(); //CharacterID -> count
                void AddC(int cid)
                {
                    characterCount.TryGetValue(cid, out var old); //Give 0 if not found.
                    characterCount[cid] = old + 1;
                }
                foreach (var v in highestVariants)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        AddC(v.ZV.CharacterIDs[i]);
                    }
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
                static bool HasOne(ZVWrapper uv, int cid)
                {
                    return uv.ZV.CharacterIndexMap.ContainsKey(cid);
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
            int[] inheritedNewComboData = null;
            InMemoryComboBorrowInfo[] inheritedNewComboBorrow = null;

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
                borrowCalculator.Run(uv1.ZV?.CharacterIndexMap, uv2.ZV?.CharacterIndexMap, uv3.ZV?.CharacterIndexMap,
                        uv1.ActualBorrowIndex, uv2.ActualBorrowIndex, uv3.ActualBorrowIndex);
                var borrow = borrowCalculator.Result;
                _comboListBuilder.AddCombo(uv1.ZV?.Index ?? -1, uv2.ZV?.Index ?? -1, uv3.ZV?.Index ?? -1, borrow);

                if (inheritComboInfo?.Match(uv1.ZV?.ZhouVariantID, uv2.ZV?.ZhouVariantID, uv3.ZV?.ZhouVariantID, ref borrow) ?? false)
                {
                    if (inheritedNewComboData is not null)
                    {
                        //More than one combo. Don't check any more.
                        inheritComboInfo = null;
                    }
                    else
                    {
                        inheritedNewComboData = new[] { uv1.ZV?.Index ?? -1, uv2.ZV?.Index ?? -1, uv3.ZV?.Index ?? -1 };
                        inheritedNewComboBorrow = borrow;
                    }
                }
            }

            //Initialize builder.
            _comboListBuilder.Reset(newComboSize);

            //Iterate and generate results (3 cases).

            //Combo with 3 Zhous.
            if (newComboSize == 3)
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
            else if (newComboSize == 2)
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
            else if (newComboSize == 1)
            {
                foreach (var x in highestVariants)
                {
                    Output(x, ZVWrapper.Null, ZVWrapper.Null);
                }
            }

            user.WriteComboList(_comboListBuilder);

            //Two null check is necessary. If user modified zhou borrow info, inheritedNewComboData
            //might be null while inheritComboInfo is not.
            if (inheritedNewComboData is not null && inheritComboInfo is not null)
            {
                user.InheritSelectedCombo(inheritedNewComboData, inheritedNewComboBorrow, inheritComboInfo.SelectedZhou);
            }

            user.LastComboCalculation = TimeZoneHelper.BeijingNow;
        }
    }
}
