﻿using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public static class CalcComboValues
    {
        public class ResultStorage
        {
            //0: OK. >0: try more bosses. <0: try less bosses.
            public int Balance { get; set; }

            public int EndBossIndex { get; set; }
            public float EndBossDamage { get; set; }
            public Dictionary<(int user, int combo), float> ComboValues { get; } = new();
            public Dictionary<int, float> BossValues { get; } = new();
        }

        public struct BossIndexInfo
        {
            public int Stage { get; init; }
            public int Lap { get; init; }
            public int Step { get; init; }

            public BossIndexInfo(int stage, int lap, int step)
            {
                Stage = stage;
                Lap = lap;
                Step = step;
            }
        }

        public class StaticInfo
        {
            public Guild Guild { get; init; }
            public List<int> FirstLapForStages { get; init; }
            public List<List<GuildBossStatus>> Bosses { get; init; }
            public Dictionary<int, float> BossPlanRatios { get; init; } //BossID -> plan ratio
            public InMemoryUser[] Users { get; set; } //with .ZhouX loaded

            public BossIndexInfo ConvertBossIndex(int bossIndex)
            {
                int stage = 0, lap = 0, iboss = 0;
                while (bossIndex >= iboss + Bosses[stage].Count)
                {
                    iboss += Bosses[stage].Count;
                    lap += 1;
                    if (stage + 1 == FirstLapForStages.Count)
                    {
                        if (Bosses[stage].Count == 0)
                        {
                            throw new Exception("Invalid boss data");
                        }
                    }
                    else if (lap >= FirstLapForStages[stage + 1])
                    {
                        stage += 1;
                    }
                }
                return new(stage, lap, bossIndex - iboss);
            }

            public int ConvertBossIndex(BossIndexInfo bossInfo)
            {
                var ret = 0;
                for (int lap = 0, stage = 0; lap < bossInfo.Lap; ++lap)
                {
                    if (stage + 1 < FirstLapForStages.Count && lap >= FirstLapForStages[stage + 1])
                    {
                        stage += 1;
                    }
                    ret += Bosses[stage].Count;
                }
                return ret + bossInfo.Step;
            }

            //lastBoss is inclusive.
            public void ListBossesInRange(BossIndexInfo firstBoss, BossIndexInfo lastBoss,
                List<(BossIndexInfo boss, int count)> results, bool firstIsSpecial)
            {
                if (firstBoss.Lap == lastBoss.Lap)
                {
                    //Simple case: single lap.
                    var stage = firstBoss.Stage;
                    var lap = firstBoss.Lap;
                    for (int i = firstBoss.Step; i <= lastBoss.Step; ++i)
                    {
                        results.Add((new(stage, lap, i), 1));
                    }
                    return;
                }

                BossIndexInfo currentRangeStart = firstBoss;

                void Purge(int endStage, int endLap) //endLap: exclusive
                {
                    var bossCount = Bosses[currentRangeStart.Stage].Count;
                    var rangeLaps = endLap - currentRangeStart.Lap;
                    for (int i = currentRangeStart.Step; i < bossCount; ++i)
                    {
                        results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps));
                    }
                    if (rangeLaps > 1)
                    {
                        for (int i = 0; i < currentRangeStart.Step; ++i)
                        {
                            results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps - 1));
                        }
                    }
                    currentRangeStart = new(endStage, endLap, 0);
                }

                //Loop from second lap. Stop before last lap.
                {
                    int stage, lap;
                    for (stage = firstBoss.Stage, lap = firstBoss.Lap + 1; lap <= lastBoss.Lap; ++lap)
                    {
                        if (stage + 1 < FirstLapForStages.Count && lap >= FirstLapForStages[stage + 1])
                        {
                            stage += 1;
                            Purge(stage, lap);
                        }
                        else if (lap == firstBoss.Lap + 1 && firstIsSpecial)
                        {
                            Purge(stage, lap);
                        }
                    }

                    ////Still need to update stage first.
                    //if (stage + 1 < FirstLapForStages.Count && lap >= FirstLapForStages[stage + 1])
                    //{
                    //    stage += 1;
                    //    Purge(stage, lap);
                    //}

                    //Purge last range. This starts from first boss, but may ends in the middle.
                    var bossCount = Bosses[currentRangeStart.Stage].Count;
                    var bossCountLast = lastBoss.Step + 1;
                    var rangeLaps = lastBoss.Lap - currentRangeStart.Lap + 1;

                    //Note that we reversed the two loops in order to put last boss last.
                    if (rangeLaps > 1)
                    {
                        for (int i = bossCountLast; i < bossCount; ++i)
                        {
                            results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps - 1));
                        }
                    }
                    for (int i = 0; i < bossCountLast; ++i)
                    {
                        results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps));
                    }
                }
            }
        }

        //Pretreat all combos from a single user. Group them by boss combination and (approximate) damage.
        //Here we assume bossID has been sorted when generating the combos, so we can compare one by one.
        //Note that this merging is before (and thus different from) another splitting (by SplitCombos).
        private class ComboMerger
        {
            public struct SpreadInfo
            {
                public int UserIndex;
                public int ComboIndex; //Index in original UserCombo[] array.
                public int ComboGroupIndex; //Merged index.
                public float Weight; //value of single combo = Weight * value of group
            }

            public unsafe struct MergedComboGroup
            {
                public (int a, int b, int c) Boss { get; }

                //Single linked-list in Result. Note that 0 is used as invalid here, because
                //the first group is never a "next" group.
                public int NextGroupIndex;

                //For the first group of each boss combination (a,b,c), this contains the count
                //of all groups in the linked list. This field in the following entries in the
                //linked list is not used (remain 1).
                public int CountInGroupChain;

                public int Count;

                //Total weight of this group relative to the total weight of all groups in the
                //linked list. This is used to initialize this groups value in calculation.
                public float RelativeWeightInChain;

                //Here vector is used instead of fixed buffer in order to accelerate weighted-
                //average calculation.
                private Vector3 _damageMin, _damageMax;

                private Vector3 _damageSum;
                private Vector3 _damageSqSum;

                //Caution: this property calculates vector division. Try to access only once.
                public Vector3 WeightedAverageDamage => _damageSqSum / _damageSum;
                public float AverageTotalDamage => (_damageSum.X + _damageSum.Y + _damageSum.Z) / Count;

                public MergedComboGroup((int a, int b, int c) boss, Vector3 damage, float initValue)
                {
                    Boss = boss;

                    //Note that we initialize the group with the damage from the first combo.
                    //This damage will be fixed to be weighted-average of all combos in the
                    //group later. This will give unstable results for different users (meaning
                    //that, if the order of combo list changes, the merging result will also
                    //change, and since that order is given by database, we expect that different
                    //users see slightly different results).
                    //We could have been more accurate, by calculating the average of all combos
                    //that have been added to this group and use the accurate value to compare,
                    //but it wouldn't lead to significantly better results.
                    _damageMin = damage * 0.95f;
                    _damageMax = damage * 1.05f;

                    _damageSum = damage;
                    _damageSqSum = damage * damage;

                    NextGroupIndex = 0;
                    Count = 1;
                    CountInGroupChain = 1;
                    RelativeWeightInChain = initValue;
                }

                public bool TryAdd(Vector3 damage, float initValue)
                {
                    var lowerBd = damage - _damageMin;
                    var upperBd = _damageMax - damage;
                    //Damage can be 0, so inclusive.
                    if (lowerBd.X >= 0 && lowerBd.Y >= 0 && lowerBd.Z >= 0 &&
                        upperBd.X >= 0 && upperBd.Y >= 0 && upperBd.Z >= 0)
                    {
                        _damageSum += damage;
                        _damageSqSum += damage * damage;
                        Count += 1;
                        RelativeWeightInChain += initValue;
                        return true;
                    }
                    return false;
                }
            }

            //Using array allows us to get a mutable reference to the struct.
            private MergedComboGroup[] _resultBuffer = new MergedComboGroup[20];
            private int _resultCount;

            //First entry for the boss (a,b,c) in Result. There can be multiple such entries,
            //forming a single linked list.
            public Dictionary<(int a, int b, int c), int> BossIndexDict = new();

            public Span<MergedComboGroup> Results => new(_resultBuffer, 0, _resultCount);

            private int AddResult(MergedComboGroup g)
            {
                //Always ensure there is one slot available.
                var ret = _resultCount++;
                _resultBuffer[ret] = g;
                if (_resultCount == _resultBuffer.Length)
                {
                    var newBuffer = new MergedComboGroup[_resultBuffer.Length * 2];
                    Array.Copy(_resultBuffer, newBuffer, _resultBuffer.Length);
                    _resultBuffer = newBuffer;
                }
                return ret;
            }

            //Same as Run, but with one single combo as input.
            public void RunSingle(int userIndex, int comboIndex, InMemoryUser.Combo combo, List<SpreadInfo> mappingOutput)
            {
                _resultCount = 0;
                BossIndexDict.Clear();

                var boss = combo.BossIDTuple;
                var dmg = combo.DamageVector;
                BossIndexDict.Add(boss, 0);
                _resultBuffer[_resultCount++] = new MergedComboGroup(boss, dmg, 1);
                mappingOutput.Add(new SpreadInfo
                {
                    UserIndex = userIndex,
                    ComboIndex = comboIndex,
                    ComboGroupIndex = 0,
                    Weight = 1f,
                });
                //Leave the only group's RelativeWeightInChain as 1.
            }

            public void Run(int userIndex, InMemoryUser input, List<float> comboInitValueBuffer,
                List<SpreadInfo> mappingOutput)
            {
                _resultCount = 0;
                BossIndexDict.Clear();

                var firstOutput = mappingOutput.Count;

                //First pass: make groups.
                for (int comboIndex = 0; comboIndex < input.TotalComboCount; ++comboIndex)
                {
                    var combo = input.GetCombo(comboIndex);
                    var boss = combo.BossIDTuple;
                    var dmg = combo.DamageVector;

                    //Use 0 if not provided. Will be corrected later.
                    var initValue = comboInitValueBuffer?[comboIndex] ?? 0;

                    if (!BossIndexDict.TryGetValue(boss, out var groupIndex))
                    {
                        groupIndex = AddResult(new MergedComboGroup(boss, dmg, initValue));
                        BossIndexDict.Add(boss, groupIndex);
                        mappingOutput.Add(new SpreadInfo
                        {
                            UserIndex = userIndex,
                            ComboIndex = comboIndex,
                            ComboGroupIndex = groupIndex,
                        });
                        continue;
                    }

                    _resultBuffer[groupIndex].CountInGroupChain += 1;
                    int lastGroupIndex = 0; //This 0 is never used.
                    do
                    {
                        if (_resultBuffer[groupIndex].TryAdd(dmg, initValue))
                        {
                            mappingOutput.Add(new SpreadInfo
                            {
                                UserIndex = userIndex,
                                ComboIndex = comboIndex,
                                ComboGroupIndex = groupIndex,
                            });
                            groupIndex = -1;
                            break;
                        }
                        lastGroupIndex = groupIndex;
                        groupIndex = _resultBuffer[groupIndex].NextGroupIndex;
                    } while (groupIndex != 0);

                    if (groupIndex == 0)
                    {
                        groupIndex = AddResult(new MergedComboGroup(boss, dmg, initValue));
                        _resultBuffer[lastGroupIndex].NextGroupIndex = groupIndex;
                        mappingOutput.Add(new SpreadInfo
                        {
                            UserIndex = userIndex,
                            ComboIndex = comboIndex,
                            ComboGroupIndex = groupIndex,
                        });
                    }
                }

                //Second pass: calculate weight of each combo relative to its combo group.
                for (int i = 0; i < input.TotalComboCount; ++i)
                {
                    var combo = input.GetCombo(i);
                    var comboTotalDamage = combo.GetZhouVariant(0).Damage +
                        combo.GetZhouVariant(1).Damage +
                        combo.GetZhouVariant(2).Damage;
                    var outputElement = mappingOutput[firstOutput + i];
                    ref var g = ref _resultBuffer[outputElement.ComboGroupIndex];
                    var groupTotalDamage = g.AverageTotalDamage * g.Count;
                    outputElement.Weight = comboTotalDamage / groupTotalDamage;
                    mappingOutput[firstOutput + i] = outputElement; //Write back (this is a List).
                }

                //Finally correct the weight of each combo group relative to its chain if initValues are
                //not provided.
                if (comboInitValueBuffer is null)
                {
                    foreach (var (_, firstGroupIndex) in BossIndexDict)
                    {
                        float sum = 0f;

                        //Calculate sum.
                        var groupIndex = firstGroupIndex;
                        do
                        {
                            ref var g = ref _resultBuffer[groupIndex];
                            sum += g.AverageTotalDamage * g.Count;
                            groupIndex = g.NextGroupIndex;
                        } while (groupIndex != 0);

                        //Calculate weight.
                        var normalize = sum == 0 ? 1f : 1f / sum;
                        groupIndex = firstGroupIndex;
                        do
                        {
                            ref var g = ref _resultBuffer[groupIndex];
                            g.RelativeWeightInChain = g.AverageTotalDamage * normalize;
                            groupIndex = g.NextGroupIndex;
                        } while (groupIndex != 0);
                    }
                }
            }
        }

        private sealed class Solver : IDisposable
        {
            private bool _disposedValue;

            public void Dispose()
            {
                if (!_disposedValue)
                {
                    _splitCombos1.Dispose();
                    _splitCombos2.Dispose();
                    _splitCombos3.Dispose();
                    _splitZVBuffer.Dispose();
                    _values.Dispose();
                    _bossBuffer.Dispose();
                    _userAdjustmentBuffer.Dispose();
                    _userBossBuffer.Dispose();
                    _disposedValue = true;
                }
            }

            public float AdjustmentCoefficient { get; set; } = 0.1f;

            public StaticInfo StaticInfo { get; set; }
            public bool FixSelectedCombo { get; set; }
            
            public BossIndexInfo FirstBoss { get; set; }
            public BossIndexInfo LastBoss { get; set; }
            public float FirstBossHp { get; set; }

            //DamageScale: Scale damage of all Zhous to allow estimating with more
            //than 1 stage. Use 1 for generating final results
            public float DamageScale { get; set; }

            private readonly List<(BossIndexInfo boss, int count)> _bosses = new();
            private int _alignedBossCount;
            private readonly List<float> _bossTotalHp = new();
            private readonly List<List<int>> _splitBossTable = new();
            private readonly Dictionary<int, int> _splitBossTableIndex = new();

            private readonly List<(int begin, int end)> _userSplitComboRange = new();
            //This list contains the SplitZV index (pointing to elements in _splitZVBoss and _splitZVDamage)
            //for each combo. Each combo has 3 items in this list.
            private FastArrayList<int> _splitCombos1 = default;
            private FastArrayList<int> _splitCombos2 = default;
            private FastArrayList<int> _splitCombos3 = default;
            //Other info of the split combos. Because these are not used in the inner loop of calculation,
            //they are separated from the _splitCombos.
            private readonly List<(int user, int comboGroup)> _splitComboInfo = new();
            private readonly List<float> _splitZVDamage = new();
            private readonly List<int> _splitZVBoss = new();
            //Used to store intermediate results for each split ZV during the calculation.
            private FastArrayList<float> _splitZVBuffer = default;
            private readonly Dictionary<(int, int), int> _splitZVMap = new(); //(split boss index, damage) -> split ZV index

            private FastArrayList<float> _values = default;
            private FastArrayList<float> _bossBuffer = default; //for both damage ratio and correction factor.
            private readonly List<float> _valueInitBuffer = new();

            private readonly Dictionary<int, float> _mergeBossHp = new();

            private FastArrayList<float> _userAdjustmentBuffer = default;
            private FastArrayList<float> _userBossBuffer = default; //Store damage of each boss from each user.
            private readonly ComboMerger _comboMerger = new();
            private readonly List<ComboMerger.SpreadInfo> _comboGroupSpreadInfoList = new();

            private readonly Dictionary<(int user, int comboGroup), float> _valueMergeIntermediate = new();

            private void ListAndSplitBosses(bool firstIsSpecial, bool lastIsSpecial)
            {
                //Note that here firstIsSpecial means first boss is special, but
                //ListBossesInRange method treats the whole lap as special. This is
                //OK. We only end up with a few more bosses in the following calc.
                _bosses.Clear();
                StaticInfo.ListBossesInRange(FirstBoss, LastBoss, _bosses, firstIsSpecial || FirstBoss.Step != 0);

                //ListBossesInRange does not handle last, but we can split it here.
                if (lastIsSpecial && _bosses[^1].count > 1)
                {
                    var b = _bosses[^1];

                    b.count -= 1;
                    _bosses[^1] = b;
                    b.count = 1;
                    _bosses.Add(b);
                }
                _alignedBossCount = (_bosses.Count + 7) & ~7; //Align to 256 bit boundary (AVX requirement).

                _splitBossTableIndex.Clear();
                _bossTotalHp.Clear();
                int maxCount = 0;
                foreach (var (boss, count) in _bosses)
                {
                    var bossObj = StaticInfo.Bosses[boss.Stage][boss.Step];
                    var bossID = bossObj.BossID;
                    if (!_splitBossTableIndex.TryGetValue(bossID, out var tableIndex))
                    {
                        tableIndex = _splitBossTableIndex.Count;
                        _splitBossTableIndex.Add(bossID, tableIndex);
                        if (_splitBossTable.Count <= tableIndex)
                        {
                            _splitBossTable.Add(new());
                        }
                        else
                        {
                            _splitBossTable[tableIndex].Clear();
                        }
                    }
                    _splitBossTable[tableIndex].Add(_bossTotalHp.Count);

                    float hp = count * bossObj.Boss.Life;
                    if (_bossTotalHp.Count == 0)
                    {
                        hp *= 1 - FirstBossHp;
                    }
                    _bossTotalHp.Add(hp / bossObj.DamageRatio / DamageScale);

                    maxCount = Math.Max(count, maxCount);
                }
            }

            private void SplitCombosAndInitValues(InitValuesDelegate initValues)
            {
                (int user, int comboGroup) currentCombo = default;
                (int a, int b, int c) splitBuffer = default;
                Span<int> splitBufferSpan = MemoryMarshal.CreateSpan(ref splitBuffer.a, 3);

                void SplitCombo(Span<int> buffer, ref ComboMerger.MergedComboGroup comboGroup,
                    Vector3 damage, float bossHpProduct, int index)
                {
                    (int boss, int damage)? zvinfo = index switch
                    {
                        0 => (comboGroup.Boss.a, (int)damage.X),
                        1 => (comboGroup.Boss.b, (int)damage.Y),
                        2 => (comboGroup.Boss.c, (int)damage.Z),
                        _ => null,
                    };
                    if (!zvinfo.HasValue)
                    {
                        for (int i = index; i < 3; ++i)
                        {
                            buffer[i] = 0;
                        }
                        _splitCombos1.Add(buffer[0]);
                        _splitCombos2.Add(buffer[1]);
                        _splitCombos3.Add(buffer[2]);

                        _splitComboInfo.Add(currentCombo);
                        _userAdjustmentBuffer.Add(0);
                        _valueInitBuffer.Add(bossHpProduct); //Here the list is used to store per group info.
                        //_values will be added later.
                        return;
                    }
                    if (!_splitBossTableIndex.TryGetValue(zvinfo.Value.boss, out var splitBossTableIndex))
                    {
                        //The boss is not included in calculation. Skip.
                        buffer[index] = 0;
                        SplitCombo(buffer, ref comboGroup, damage, bossHpProduct, index + 1);
                    }
                    else
                    {
                        foreach (var splitBossID in _splitBossTable[splitBossTableIndex])
                        {
                            //Note that the split ZV does not distinguish ZVs with same damage on same
                            //boss. This including the merged ZVs (whose damages have been averaged and 
                            //converted to int).
                            if (!_splitZVMap.TryGetValue(zvinfo.Value, out var splitZVIndex))
                            {
                                splitZVIndex = _splitZVBoss.Count;
                                _splitZVBoss.Add(splitBossID);
                                _splitZVDamage.Add(zvinfo.Value.damage / _bossTotalHp[splitBossID]);
                                _splitZVBuffer.Add(0);
                                _splitZVMap[zvinfo.Value] = splitZVIndex;
                            }
                            buffer[index] = splitZVIndex;
                            SplitCombo(buffer, ref comboGroup, damage,
                                bossHpProduct * _bossTotalHp[splitBossID] / 1_000_000f,
                                index + 1);
                        }
                    }
                }

                _splitZVMap.Clear();
                _splitCombos1.Clear();
                _splitCombos2.Clear();
                _splitCombos3.Clear();
                _splitComboInfo.Clear();
                _splitZVDamage.Clear();
                _splitZVBoss.Clear();
                _splitZVBuffer.Clear();
                _userSplitComboRange.Clear();
                _userAdjustmentBuffer.Clear();
                _comboGroupSpreadInfoList.Clear();
                _values.Clear();
                _userBossBuffer.Clear();
                _userBossBuffer.EnsureSize(StaticInfo.Users.Length * _alignedBossCount, updateCount: true);

                //Add an empty splitZV at index 0.
                _splitZVBoss.Add(0);
                _splitZVDamage.Add(0);
                _splitZVBuffer.Add(0);

                for (int userIndex = 0; userIndex < StaticInfo.Users.Length; ++userIndex)
                {
                    var user = StaticInfo.Users[userIndex];
                    var userRangeBegin = _splitComboInfo.Count;

                    //Get init value if provided.
                    //Here _valueInitBuffer is used to store initial values of each combo of the user.
                    _valueInitBuffer.Clear();
                    bool useInitValue = initValues?.Invoke(StaticInfo, userIndex, _valueInitBuffer) ?? false;

                    if (FixSelectedCombo && user.SelectedComboIndex != -1)
                    {
                        //Make a group containing one combo.
                        _comboMerger.RunSingle(userIndex, user.SelectedComboIndex,
                            user.GetCombo(user.SelectedComboIndex), _comboGroupSpreadInfoList);
                    }
                    else
                    {
                        _comboMerger.Run(userIndex, user,
                            useInitValue ? _valueInitBuffer : null, //Only provide if we got it from the delegate.
                            _comboGroupSpreadInfoList);
                    }
                    //Up to here info in _valueInitBuffer has been included into merger. We will use this list
                    //in the following loop to store boss hp.

                    //Prepare for calculating a uniform value.
                    var totalValueForEachGroupChain = 1f / _comboMerger.BossIndexDict.Count;

                    foreach (var (bosses, firstGroupIndex) in _comboMerger.BossIndexDict)
                    {
                        var groupIndex = firstGroupIndex;
                        do
                        {
                            ref var group = ref _comboMerger.Results[groupIndex];
                            currentCombo = (userIndex, groupIndex);

                            var splitStartIndex = _splitComboInfo.Count;
                            _valueInitBuffer.Clear();
                            SplitCombo(splitBufferSpan, ref group, group.WeightedAverageDamage, 1f, 0);

                            //Calculate normalization coefficient from boss hp product.
                            var sumBossHpProduct = 0f;
                            for (int i = 0; i < _valueInitBuffer.Count; ++i)
                            {
                                sumBossHpProduct += _valueInitBuffer[i];
                            }
                            var splitComboNormalize = 1f / sumBossHpProduct;

                            //Add init values.
                            float totalValueForGroup;
                            if (useInitValue)
                            {
                                //The group's value is the sum of all combos. This has been done by the merger and the
                                //results stored in group.RelativeWeightInChain.
                                totalValueForGroup = group.RelativeWeightInChain;
                            }
                            else
                            {
                                //Use a uniform value.
                                totalValueForGroup = totalValueForEachGroupChain * group.RelativeWeightInChain;
                            }
                            for (int i = 0; i < _valueInitBuffer.Count; ++i)
                            {
                                _values.Add(_valueInitBuffer[i] * splitComboNormalize * totalValueForGroup);
                            }

                            groupIndex = _comboMerger.Results[groupIndex].NextGroupIndex;
                        } while (groupIndex != 0);
                    }

                    _userSplitComboRange.Add((userRangeBegin, _splitComboInfo.Count));

                    //Align to 256 bit boundary (AVX requirement).
                    //Note that these values will not be read/written.
                    var alignedCount = _splitComboInfo.Count;
                    alignedCount = (alignedCount + 7) & ~7;
                    while (_splitComboInfo.Count < alignedCount)
                    {
                        _splitCombos1.Add(0);
                        _splitCombos2.Add(0);
                        _splitCombos3.Add(0);
                        _splitComboInfo.Add((0, 0));
                        _userAdjustmentBuffer.Add(0);
                        _values.Add(0);
                    }
                }

                //Init boss buffer.
                while (_bossBuffer.Count < _bosses.Count)
                {
                    _bossBuffer.Add(0);
                }
            }

            //Remove combos whose values are lower than valueRatio*maxValue.
            private void Compress(float valueRatio)
            {
                int compressPointer = 0;
                for (int i = 0; i < _userSplitComboRange.Count; ++i)
                {
                    var (begin, end) = _userSplitComboRange[i];
                    var currentUserNewBegin = compressPointer;

                    //First pass: find max value.
                    var maxValue = 0f;
                    for (int j = begin; j < end; ++j)
                    {
                        maxValue = MathF.Max(maxValue, _values[j]);
                    }
                    var removeThreshold = maxValue * valueRatio;

                    //Second pass: compress, removing lower values.
                    for (int j = begin; j < end; ++j)
                    {
                        if (_values[j] > removeThreshold)
                        {
                            _values[compressPointer] = _values[j];
                            _splitCombos1[compressPointer] = _splitCombos1[j];
                            _splitCombos2[compressPointer] = _splitCombos2[j];
                            _splitCombos3[compressPointer] = _splitCombos3[j];
                            //No need to move _userAdjustmentBuffer because it's updated in each step.

                            compressPointer += 1;
                        }
                    }

                    //Align.
                    //Note that we don't clear the data in the padding. They are not used.
                    compressPointer = (compressPointer + 7) & ~7;

                    _userSplitComboRange[i] = (currentUserNewBegin, compressPointer);
                }

                _values.Truncate(compressPointer);
                _splitCombos1.Truncate(compressPointer);
                _splitCombos2.Truncate(compressPointer);
                _splitCombos3.Truncate(compressPointer);
                _userAdjustmentBuffer.Truncate(compressPointer);
            }

            private unsafe int CalculateDamage()
            {
                //_bossBuffer used as damage ratio.
                fixed (float* splitZVBufferPtr = &_splitZVBuffer.DataRef, userBossBufferPtr = &_userBossBuffer.DataRef,
                    bossBufferPtr = &_bossBuffer.DataRef)
                {
                    fixed (int* splitComboPtr1 = &_splitCombos1[0], splitComboPtr2 = &_splitCombos2[0],
                        splitComboPtr3 = &_splitCombos3[0])
                    {
                        for (int i = 0; i < _bosses.Count; ++i)
                        {
                            bossBufferPtr[i] = 0;
                        }

                        for (int i = 0; i < _userSplitComboRange.Count; ++i)
                        {
                            for (int j = 0; j < _splitZVBoss.Count; ++j)
                            {
                                splitZVBufferPtr[j] = 0;
                            }

                            //Merge SplitZV values.
                            //TODO SIMD (need a reverse matrix of splitComboPtr)
                            var (begin, end) = _userSplitComboRange[i];
                            for (int j = begin; j < end; ++j)
                            {
                                var v = _values[j];
                                splitZVBufferPtr[splitComboPtr1[j]] += v;
                                splitZVBufferPtr[splitComboPtr2[j]] += v;
                                splitZVBufferPtr[splitComboPtr3[j]] += v;
                            }

                            //Calculate user's damage in _userBossBuffer (we need these values in adjustment step).
                            float* userBossBufferSegmentPtr = &userBossBufferPtr[_alignedBossCount * i];
                            for (int j = 0; j < _bosses.Count; ++j)
                            {
                                userBossBufferSegmentPtr[j] = 0;
                            }
                            for (int j = 0; j < _splitZVBoss.Count; ++j)
                            {
                                userBossBufferSegmentPtr[_splitZVBoss[j]] += _splitZVDamage[j] * _splitZVBuffer[j];
                            }

                            //Merge to _bossBuffer.
                            for (int j = 0; j < _bosses.Count; ++j)
                            {
                                bossBufferPtr[j] += userBossBufferSegmentPtr[j];
                            }
                        }
                    }
                }

                bool allPositive = true, allNegative = true;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    if (_bossBuffer[i] < 0.8f && i < _bosses.Count - 1) return -1; //If any boss has damage lower than 80%, it's considered not possible.
                    if (allPositive && _bossBuffer[i] < 0.99f && i < _bosses.Count - 1) allPositive = false;
                    if (allNegative && _bossBuffer[i] > 1.01f && i < _bosses.Count - 1) allNegative = false;
                }
                return allPositive ? 1 : allNegative ? -1 : 0;
            }

            private void AdjustAverage()
            {
                //_bossBuffer used for adjustment
                var averageRatio = 1f;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    averageRatio += _bossBuffer[i];
                }
                averageRatio /= _bosses.Count;
                averageRatio *= 1.1f; //Make it a little bit larger.

                var totalAdjustment = 0f;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    var adjustment = (_bossBuffer[i] - averageRatio) * 0.01f; //Adjust everything to average.
                    _bossBuffer[i] = adjustment;
                    totalAdjustment = MathF.Max(totalAdjustment, _bossBuffer[i]);
                }

                for (int i = 0; i < _bosses.Count; ++i)
                {
                    _bossBuffer[i] -= totalAdjustment;
                }

                for (int i = 0; i < _userSplitComboRange.Count; ++i) //User
                {
                    var userTotalValue = 0f;
                    var (begin, end) = _userSplitComboRange[i];
                    for (int j = begin; j < end; ++j)
                    {
                        var comboAdjustment = 0f;

                        var splitZVIndex1 = _splitCombos1[j];
                        var splitZVIndex2 = _splitCombos2[j];
                        var splitZVIndex3 = _splitCombos3[j];
                        comboAdjustment += _bossBuffer[_splitZVBoss[splitZVIndex1]] * _splitZVDamage[splitZVIndex1];
                        comboAdjustment += _bossBuffer[_splitZVBoss[splitZVIndex2]] * _splitZVDamage[splitZVIndex2];
                        comboAdjustment += _bossBuffer[_splitZVBoss[splitZVIndex3]] * _splitZVDamage[splitZVIndex3];

                        _values[j] -= comboAdjustment;
                        if (_values[j] < 0)
                        {
                            _values[j] = 0;
                        }
                        userTotalValue += _values[j];
                    }
                    if (userTotalValue < 1)
                    {
                        //Normalize by addition.
                        var normalize = (userTotalValue - 1) / (end - begin);
                        for (int j = begin; j < end; ++j)
                        {
                            _values[j] -= normalize;
                        }
                    }
                    else
                    {
                        //Normalize by multiplication.
                        var normalize = 1f / userTotalValue;
                        for (int j = begin; j < end; ++j)
                        {
                            _values[j] *= normalize;
                        }
                    }
                }
            }

            private unsafe void AdjustGradient(float learningRate)
            {
                const float GradientLastBoss = 0.02f;
                const float GradientOverKilledBoss1 = 0.01f;
                const float GradientOverKilledBoss2 = 0.5f;
                float deltaRatio = learningRate / (1 + learningRate);

                //_bossBuffer used for adjustment
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    _bossBuffer[i] = i == _bosses.Count - 1 ? GradientLastBoss :
                        _bossBuffer[i] switch
                        {
                            > 1.005f => GradientOverKilledBoss1,
                            > 1.0f => GradientOverKilledBoss2,
                            > 0.5f => 1f,
                            _ => 2f,
                        };
                }

                //Adjustment coefficient for each SplitZV.
                for (int i = 0; i < _splitZVBoss.Count; ++i)
                {
                    _splitZVBuffer[i] = deltaRatio * _bossBuffer[_splitZVBoss[i]] * _splitZVDamage[i];
                }

                fixed (float* adjustmentBufferPtr = &_userAdjustmentBuffer[0], userBossBufferPtr = &_userBossBuffer.DataRef,
                    splitZVBufferPtr = &_splitZVBuffer[0], valuePtr = &_values[0], bossBufferPtr = &_bossBuffer.DataRef)
                {
                    fixed (int* splitCombosPtr1 = &_splitCombos1[0], splitCombosPtr2 = &_splitCombos2[0],
                        splitCombosPtr3 = &_splitCombos3[0])
                    {
                        for (int i = 0; i < _userSplitComboRange.Count; ++i) //User
                        {
                            //Calculate user's contribution to the objective function.
                            var userObjectiveFunc = 0f;
                            float* userBossBufferSegmentPtr = &userBossBufferPtr[_alignedBossCount * i];
                            for (int j = 0; j < _bosses.Count; ++j)
                            {
                                userObjectiveFunc += userBossBufferSegmentPtr[j] * bossBufferPtr[j];
                            }
                            userObjectiveFunc *= deltaRatio;

                            var (begin, end) = _userSplitComboRange[i];

                            //Calculate adjustment (gradient).
                            SimdHelper.CalculateGradient(splitZVBufferPtr,
                                &splitCombosPtr1[begin], &splitCombosPtr2[begin], &splitCombosPtr3[begin],
                                userObjectiveFunc, &adjustmentBufferPtr[begin], end - begin);

                            //Update values.
                            var userTotalValue = SimdHelper.UpdateValueSum(&valuePtr[begin],
                                &adjustmentBufferPtr[begin], end - begin);

                            if (userTotalValue == 0f)
                            {
                                throw new Exception("Normalize zero value");
                            }

                            //Normalize by multiplication.
                            var normalize = 1f / userTotalValue;
                            SimdHelper.MultiplyConst(&valuePtr[begin], normalize, end - begin);
                        }
                    }
                }
            }

            //When called from outside this class: this method assumes _bossBuffer stores damage.
            public void Merge(ResultStorage result)
            {
                result.BossValues.Clear();
                _mergeBossHp.Clear();
                for (int i = 0; i < _bosses.Count - 1; ++i) //exclude last boss
                {
                    var bossInfo = _bosses[i].boss;
                    var bossID = StaticInfo.Bosses[bossInfo.Stage][bossInfo.Step].BossID;
                    _mergeBossHp.TryGetValue(bossID, out var totalHp);
                    _mergeBossHp[bossID] = totalHp + _bossTotalHp[i];
                    result.BossValues.TryGetValue(bossID, out var totalDamage); //get 0 for new.
                    result.BossValues[bossID] = totalDamage + _bossBuffer[i] * _bossTotalHp[i];
                }
                foreach (var (k, v) in _mergeBossHp)
                {
                    result.BossValues[k] = result.BossValues[k] / v;
                }

                //Merge to get values of each combo group.
                _valueMergeIntermediate.Clear();
                for (int i = 0; i < _values.Count; ++i)
                {
                    var c = _splitComboInfo[i];
                    _valueMergeIntermediate.TryGetValue(c, out var oldValue);
                    _valueMergeIntermediate[c] = oldValue + _values[i];
                }

                //Split values of each group into combos.
                result.ComboValues.Clear();
                foreach (var info in _comboGroupSpreadInfoList)
                {
                    _valueMergeIntermediate.TryGetValue((info.UserIndex, info.ComboGroupIndex), out var groupValue);
                    result.ComboValues.Add((info.UserIndex, info.ComboIndex), info.Weight * groupValue);
                }

                result.EndBossIndex = StaticInfo.ConvertBossIndex(LastBoss);
                result.EndBossDamage = MathF.Min(1, _bossBuffer[_bosses.Count - 1]);
            }

            public void Run(ResultStorage result, bool forceMergeResults)
            {
                result.BossValues.Clear();
                result.ComboValues.Clear();
                result.EndBossDamage = 0f;

                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: true);
                SplitCombosAndInitValues(initValues: null);

                int damage;
                float learningRate = 0.01f;
                for (int i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 500; ++j)
                    {
                        CalculateDamage();
                        AdjustGradient(learningRate);
                    }
                    if ((damage = CalculateDamage()) > 0)
                    {
                        result.Balance = damage;
                        return;
                    }
                    Compress(0.05f);

                    if (i == 1)
                    {
                        learningRate = 0.005f;
                    }
                }

                damage = CalculateDamage();

                result.Balance = damage;
                if (damage == 0 || forceMergeResults)
                {
                    Merge(result);
                }
            }

            public float RunEstimate()
            {
                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: false);
                SplitCombosAndInitValues(initValues: null);

                for (int i = 0; i < 10; ++i)
                {
                    CalculateDamage();
                    AdjustAverage();
                }
                CalculateDamage();

                var total = 0f;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    total += _bossBuffer[i];
                }
                return total / _bosses.Count;
            }

            //Try initialize the buffer with values for each combo for the user. Returns false if fails (fallback to
            //same value for each combo chain).
            //Implementation does not know about combo group or combo chain and write values in the same order as
            //StaticInfo.Users.
            public delegate bool InitValuesDelegate(StaticInfo staticInfo, int userIndex, List<float> buffer);

            //Run approximately, with given initial values (given by delegate).
            public void RunApproximate(InitValuesDelegate initValues)
            {
                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: false);
                SplitCombosAndInitValues(initValues);

                float learningRate = 0.01f;
                for (int i = 0; i < 1000; ++i)
                {
                    CalculateDamage();
                    AdjustGradient(learningRate);
                }
            }

            //Initial values from the result of another calculation.
            //If the combo count of a user changes, initalize with same values.
            public static InitValuesDelegate InitValues_FromResult(ResultStorage result)
            {
                return (StaticInfo staticInfo, int userIndex, List<float> buffer) =>
                {
                    var newCount = staticInfo.Users[userIndex].TotalComboCount;
                    if (result.ComboValues.ContainsKey((userIndex, newCount - 1)) &&
                        !result.ComboValues.ContainsKey((userIndex, newCount)))
                    {
                        //Count matches. Direct copy.
                        for (int i = 0; i < newCount; ++i)
                        {
                            buffer.Add(result.ComboValues[(userIndex, i)]);
                        }
                        return true;
                    }
                    //Leave buffer uninitialized (caller will give each combo group same value).
                    return false;
                };
            }

            //Get init values from combo.Value, except for the combos of the last user.
            public static bool InitValues_FromComboCurrentValuesExceptLast(StaticInfo staticInfo, int userIndex,
                List<float> buffer)
            {
                if (userIndex != staticInfo.Users.Length - 1)
                {
                    foreach (var c in staticInfo.Users[userIndex].AllCombos)
                    {
                        buffer.Add(c.CurrentValue);
                    }
                    return true;
                }
                return false;
            }

            public static bool InitValues_FromComboTotalValuesExceptLast(StaticInfo staticInfo, int userIndex,
                List<float> buffer)
            {
                if (userIndex != staticInfo.Users.Length - 1)
                {
                    foreach (var c in staticInfo.Users[userIndex].AllCombos)
                    {
                        buffer.Add(c.TotalValue);
                    }
                    return true;
                }
                return false;
            }
        }

        //Run for all users in the guild.
        public static async Task RunAllAsync(ApplicationDbContext context, Guild guild, InMemoryGuild imGuild)
        {
            await context.GuildBossStatuses
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == false)
                .DeleteFromQueryAsync();

            var allUsers = imGuild.Members.ToArray();

            var stages = await context.BattleStages
                .OrderBy(s => s.StartLap)
                .ToListAsync();
            var stageLapList = new List<int>();
            var bossList = new List<List<GuildBossStatus>>();
            for (int i = 0; i < stages.Count; ++i)
            {
                stageLapList.Add(stages[i].StartLap);
                bossList.Add(new());
            }

            var bossPlans = await context.GuildBossStatuses
                .Include(s => s.Boss)
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == true)
                .ToListAsync();
            var planRatios = new Dictionary<int, float>();
            foreach (var boss in bossPlans)
            {
                var stageID = boss.Boss.BattleStageID;
                var stage = stages.FindIndex(s => s.BattleStageID == stageID);
                bossList[stage].Add(boss);
                planRatios.Add(boss.BossID, boss.DamageRatio);
            }
            foreach (var bossListStage in bossList)
            {
                bossListStage.Sort((b1, b2) => b1.Boss.BossID - b2.Boss.BossID);
            }

            var staticInfo = new StaticInfo()
            {
                FirstLapForStages = stageLapList,
                Bosses = bossList,
                BossPlanRatios = planRatios,
                Users = allUsers,
                Guild = guild,
            };

            //Accurate.
            var currentResult = Run(staticInfo, true);

            //TODO approximate
            //copy from accurate: all non-fixed
            //fix: none
            var totalResult = Run(staticInfo, false);

            //TODO approximate, grouped
            //copy from accurate: all users except the group
            //fix: all users except the group
            //----

            for (int userIndex = 0; userIndex < allUsers.Length; ++userIndex)
            {
                var user = allUsers[userIndex];
                for (int comboIndex = 0; comboIndex < user.TotalComboCount; ++ comboIndex)
                {
                    var comboID = (userIndex, comboIndex);
                    var c = user.GetCombo(comboIndex);
                    if (totalResult.ComboValues.TryGetValue(comboID, out var totalValue))
                    {
                        c.TotalValue = totalValue;
                    }
                    else
                    {
                        c.TotalValue = 0;
                    }
                    if (currentResult.ComboValues.TryGetValue(comboID, out var value))
                    {
                        c.CurrentValue = value;
                    }
                    else
                    {
                        c.CurrentValue = 0;
                    }
                }
            }
            foreach (var (bossID, value) in currentResult.BossValues)
            {
                var newStatus = new GuildBossStatus
                {
                    GuildID = guild.GuildID,
                    BossID = bossID,
                    DamageRatio = value,
                    IsPlan = false,
                    //TODO extract more results
                };
                context.GuildBossStatuses.Add(newStatus);
            }
            guild.PredictBossIndex = currentResult.EndBossIndex;
            guild.PredictBossDamageRatio = currentResult.EndBossDamage;
            guild.LastCalculation = TimeZoneHelper.BeijingNow;
            foreach (var user in allUsers)
            {
                user.IsValueApproximate = false;
            }
        }

        //Run for a single user and get approximate results. This is useful to show some number quickly after
        //the user refreshes the combo list.
        public static async Task RunSingleAsync(ApplicationDbContext context, Guild guild, InMemoryGuild imGuild,
            PcrIdentityUser user)
        {
            var imUser = imGuild.GetUserById(user.Id);
            var allUsers = imGuild.Members
                .Where(u => u.UserID != user.Id)
                .Concat(Enumerable.Repeat(imUser, 1)) //Put the user last.
                .ToArray();

            var stages = await context.BattleStages
                .OrderBy(s => s.StartLap)
                .ToListAsync();
            var stageLapList = new List<int>();
            var bossList = new List<List<GuildBossStatus>>();
            for (int i = 0; i < stages.Count; ++i)
            {
                stageLapList.Add(stages[i].StartLap);
                bossList.Add(new());
            }

            var bossPlans = await context.GuildBossStatuses
                .Include(s => s.Boss)
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == true)
                .ToListAsync();
            var planRatios = new Dictionary<int, float>();
            foreach (var boss in bossPlans)
            {
                var stageID = boss.Boss.BattleStageID;
                var stage = stages.FindIndex(s => s.BattleStageID == stageID);
                bossList[stage].Add(boss);
                planRatios.Add(boss.BossID, boss.DamageRatio);
            }
            foreach (var bossListStage in bossList)
            {
                bossListStage.Sort((b1, b2) => b1.Boss.BossID - b2.Boss.BossID);
            }

            var zhous = await context.Zhous
                .Include(z => z.Variants)
                .Where(z => z.GuildID == guild.GuildID)
                .ToListAsync();
            var variants = zhous.SelectMany(z => z.Variants).ToDictionary(v => v.ZhouVariantID);

            var staticInfo = new StaticInfo()
            {
                FirstLapForStages = stageLapList,
                Bosses = bossList,
                BossPlanRatios = planRatios,
                Users = allUsers,
                Guild = guild,
            };

            var currentResult = RunApproximate(staticInfo, true, guild, Solver.InitValues_FromComboCurrentValuesExceptLast);
            var totalResult = RunApproximate(staticInfo, false, guild, Solver.InitValues_FromComboTotalValuesExceptLast);

            for (int comboIndex = 0; comboIndex < imUser.TotalComboCount; ++comboIndex)
            {
                var comboID = (allUsers.Length - 1, comboIndex);
                var c = imUser.GetCombo(comboIndex);
                if (totalResult.ComboValues.TryGetValue(comboID, out var totalValue))
                {
                    c.TotalValue = totalValue;
                }
                else
                {
                    c.TotalValue = 0;
                }
                if (currentResult.ComboValues.TryGetValue(comboID, out var value))
                {
                    c.CurrentValue = value;
                }
                else
                {
                    c.CurrentValue = 0;
                }
            }

            imUser.IsValueApproximate = true;
        }

        public static ResultStorage Run(StaticInfo staticInfo, bool fixSelected)
        {
            using var solver = new Solver
            {
                StaticInfo = staticInfo,
                FixSelectedCombo = fixSelected,
            };

            var result = new ResultStorage();
            solver.DamageScale = 1f;

            var firstBoss = staticInfo.ConvertBossIndex(staticInfo.Guild.BossIndex);
            var totalPower = 1f;
            if (staticInfo.Guild.BossDamageRatio != 0 || firstBoss.Step != 0)
            {
                var lastBossStep = staticInfo.Bosses[firstBoss.Stage].Count - 1;

                //Close the current lap.
                //We no longer use estimated value to do this.

                //AdjustAverage cannot handle this case. It will sometimes reports less than 1 ratio
                //although when fully optimized the value can be very large. This is a problem especially
                //when the current lap only has one or two bosses left.

                firstBoss = staticInfo.ConvertBossIndex(staticInfo.Guild.BossIndex + (lastBossStep - firstBoss.Step) + 1);
            }

            //Run full laps.
            for (int stage = firstBoss.Stage; ; ++stage)
            {
                solver.DamageScale = totalPower;
                solver.FirstBoss = firstBoss;
                solver.LastBoss = new(stage, firstBoss.Lap, staticInfo.Bosses[firstBoss.Stage].Count - 1);
                var estimatedLaps = solver.RunEstimate();
                var nextStageStart = stage == staticInfo.FirstLapForStages.Count - 1 ?
                    int.MaxValue : staticInfo.FirstLapForStages[stage + 1];
                var numLapsInThisStage = nextStageStart - firstBoss.Lap;
                if (estimatedLaps < numLapsInThisStage)
                {
                    //Can't finish current stage.
                    var actualLaps = (int)MathF.Floor(estimatedLaps);
                    firstBoss = new(stage, firstBoss.Lap + actualLaps, 0);
                    break;
                }
                else
                {
                    firstBoss = new(stage + 1, staticInfo.FirstLapForStages[stage + 1], 0);
                    totalPower -= numLapsInThisStage / estimatedLaps * totalPower;
                }
            }

            //Now we have a basic estimation. Run with binary search.
            const int InitStep = 8;
            var firstBossIndex = staticInfo.Guild.BossIndex;
            var lastBossIndex = staticInfo.ConvertBossIndex(firstBoss) + 2; //estimated lap (middle).
            var searchStep = InitStep;
            int? lastBalance = null;

            solver.DamageScale = 1.0f;
            if (staticInfo.Guild.BossDamageRatio >= 0.99f)
            {
                firstBossIndex += 1;
                solver.FirstBossHp = 0;
            }
            else
            {
                solver.FirstBossHp = staticInfo.Guild.BossDamageRatio;
            }

            do
            {
                solver.FirstBoss = staticInfo.ConvertBossIndex(firstBossIndex);
                solver.LastBoss = staticInfo.ConvertBossIndex(lastBossIndex);
                solver.Run(result, false);
                if (result.Balance == 0)
                {
                    return result;
                }
                else
                {
                    if (searchStep != InitStep || lastBalance.HasValue && lastBalance != result.Balance)
                    {
                        searchStep /= 2;
                    }
                    if (result.Balance > 0)
                    {
                        lastBossIndex += searchStep;
                    }
                    else
                    {
                        lastBossIndex -= searchStep;
                        if (lastBossIndex < firstBossIndex)
                        {
                            lastBossIndex = firstBossIndex;
                            searchStep = 1;
                        }
                    }
                }
                lastBalance = result.Balance;
            } while (searchStep > 0);

            if (lastBalance < 0)
            {
                //We stopped at a (-1) state. Better move back 1 step.
                solver.LastBoss = staticInfo.ConvertBossIndex(lastBossIndex - 1);
                solver.Run(result, false);
            }

            solver.Merge(result);
            return result;
        }

        private static ResultStorage RunApproximate(StaticInfo staticInfo, bool fixSelected,
            Guild guild, Solver.InitValuesDelegate initValues)
        {
            using var solver = new Solver
            {
                StaticInfo = staticInfo,
                FixSelectedCombo = fixSelected,

                FirstBoss = staticInfo.ConvertBossIndex(guild.BossIndex),
                FirstBossHp = guild.BossDamageRatio,
                LastBoss = staticInfo.ConvertBossIndex(guild.PredictBossIndex),

                DamageScale = 1f,
            };
            solver.RunApproximate(initValues);

            var result = new ResultStorage();
            solver.Merge(result);
            return result;
        }
    }
}
