﻿using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            public UserCombo[][] Users { get; set; } //with .ZhouX loaded

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
                    for (stage = firstBoss.Stage, lap = firstBoss.Lap + 1; lap < lastBoss.Lap; ++lap)
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

                    //Still need to update stage first.
                    if (stage + 1 < FirstLapForStages.Count && lap >= FirstLapForStages[stage + 1])
                    {
                        stage += 1;
                        Purge(stage, lap);
                    }

                    //Purge last range. This starts from first boss, but may ends in the middle.
                    var bossCount = Bosses[currentRangeStart.Stage].Count;
                    var bossCountLast = lastBoss.Step + 1;
                    var rangeLaps = lastBoss.Lap - currentRangeStart.Lap + 1;

                    //Note that we reversed the two loops in order to put last boss last.
                    if (rangeLaps > 0)
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

        private class Solver
        {
            private unsafe struct SplitComboInfo
            {
                //We process on non-saved combo entities, so we can't rely on
                //primary key.
                public (int user, int combo) ComboID;
                private fixed int _boss[3];
                private fixed float _damage[3];

                public Span<int> Boss => MemoryMarshal.CreateSpan(ref _boss[0], 3);
                public Span<float> Damage => MemoryMarshal.CreateSpan(ref _damage[0], 3);
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
            private readonly List<float> _bossTotalHp = new();
            private readonly List<List<int>> _splitBossTable = new();
            private readonly Dictionary<int, int> _splitBossTableIndex = new();

            private readonly List<SplitComboInfo> _splitCombos = new();
            private readonly List<int> _userFirstSplitComboIndex = new();

            private readonly List<float> _values = new();
            private readonly List<float> _bossBuffer = new(); //for both damage ratio and correction factor.
            private readonly List<float> _valueInitBuffer = new();
            private readonly List<int> _valueInitSplitCount = new();

            private readonly Dictionary<int, float> _mergeBossHp = new();

            private readonly List<float> _userAdjustmentBuffer = new();

            private void ListAndSplitBosses(bool firstIsSpecial, bool lastIsSpecial)
            {
                //Note that here firstIsSpecial means first boss is special, but
                //ListBossesInRange method treats the whole lap as special. This is
                //OK. We only end up with a few more bosses in the following calc.
                _bosses.Clear();
                StaticInfo.ListBossesInRange(FirstBoss, LastBoss, _bosses, firstIsSpecial);

                //ListBossesInRange does not handle last, but we can split it here.
                if (lastIsSpecial && _bosses[^1].count > 1)
                {
                    var b = _bosses[^1];

                    b.count -= 1;
                    _bosses[^1] = b;
                    b.count = 1;
                    _bosses.Add(b);
                }

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
                        hp -= FirstBossHp;
                    }
                    _bossTotalHp.Add(hp / bossObj.DamageRatio / DamageScale);

                    maxCount = Math.Max(count, maxCount);
                }
            }

            private void SplitCombos()
            {
                SplitComboInfo buffer = default;
                void SplitCombo(ref SplitComboInfo buffer, UserCombo combo, int index)
                {
                    (int boss, int damage)? zvinfo = index switch
                    {
                        0 => (combo.Boss1, combo.Damage1),
                        1 => (combo.Boss2, combo.Damage2),
                        2 => (combo.Boss3, combo.Damage3),
                        _ => null,
                    };
                    if (!zvinfo.HasValue)
                    {
                        for (int i = index; i < 3; ++i)
                        {
                            buffer.Boss[i] = 0;
                            buffer.Damage[i] = 0;
                        }
                        _splitCombos.Add(buffer);
                        _userAdjustmentBuffer.Add(0);
                        return;
                    }
                    if (!_splitBossTableIndex.TryGetValue(zvinfo.Value.boss, out var splitBossTableIndex))
                    {
                        //The boss is not included in calculation. Skip.
                        buffer.Boss[index] = 0;
                        buffer.Damage[index] = 0;
                        SplitCombo(ref buffer, combo, index + 1);
                    }
                    else
                    {
                        foreach (var splitBossID in _splitBossTable[splitBossTableIndex])
                        {
                            buffer.Boss[index] = splitBossID;
                            buffer.Damage[index] = zvinfo.Value.damage / _bossTotalHp[splitBossID];
                            SplitCombo(ref buffer, combo, index + 1);
                        }
                    }
                }
                _splitCombos.Clear();
                _userFirstSplitComboIndex.Clear();
                _userAdjustmentBuffer.Clear();
                for (int userIndex = 0; userIndex < StaticInfo.Users.Length; ++userIndex)
                {
                    var user = StaticInfo.Users[userIndex];
                    _userFirstSplitComboIndex.Add(_splitCombos.Count);
                    if (FixSelectedCombo)
                    {
                        var selected = Array.FindIndex(user, c => c.SelectedZhou.HasValue);
                        if (selected != -1)
                        {
                            //Only add one combo.
                            buffer.ComboID = (userIndex, selected);
                            SplitCombo(ref buffer, user[selected], 0);
                            continue;
                        }
                    }
                    for (int comboIndex = 0; comboIndex < user.Length; ++ comboIndex)
                    {
                        buffer.ComboID = (userIndex, comboIndex);
                        SplitCombo(ref buffer, user[comboIndex], 0);
                    }
                }
                _userFirstSplitComboIndex.Add(_splitCombos.Count);
            }

            private void InitValues()
            {
                _values.Clear();
                for (int i = 1; i < _userFirstSplitComboIndex.Count; ++i)
                {
                    var userCombos = _userFirstSplitComboIndex[i] - _userFirstSplitComboIndex[i - 1];
                    var val = 1f / userCombos;
                    for (int j = 0; j < userCombos; ++j)
                    {
                        _values.Add(val);
                    }
                }
                while (_bossBuffer.Count < _bosses.Count)
                {
                    _bossBuffer.Add(0);
                }
            }

            private void InitValues(InitValuesDelegate valueSource)
            {
                _values.Clear();
                for (int i = 1; i < _userFirstSplitComboIndex.Count; ++i)
                {
                    _valueInitBuffer.Clear();
                    _valueInitSplitCount.Clear();
                    valueSource(StaticInfo, i - 1, _valueInitBuffer);

                    var begin = _userFirstSplitComboIndex[i - 1];
                    var end = _userFirstSplitComboIndex[i];

                    //Count each combo.
                    for (int j = begin; j < end; ++j)
                    {
                        var comboIndex = _splitCombos[j].ComboID.combo;
                        while (_valueInitSplitCount.Count <= comboIndex)
                        {
                            _valueInitSplitCount.Add(0);
                        }
                        _valueInitSplitCount[comboIndex] += 1;
                    }
                    //Calculate split average values.
                    for (int j = 0; j < _valueInitBuffer.Count; ++j)
                    {
                        _valueInitBuffer[j] /= _valueInitSplitCount[j];
                    }
                    //Set the values.
                    for (int j = begin; j < end; ++j)
                    {
                        _values.Add(_valueInitBuffer[_splitCombos[j].ComboID.combo]);
                    }
                }
                while (_bossBuffer.Count < _bosses.Count)
                {
                    _bossBuffer.Add(0);
                }
            }

            //Remove combos whose values are lower than valueRatio*maxValue.
            private void Compress(float valueRatio)
            {
                int compressPointer = 0;
                for (int i = 0; i < _userFirstSplitComboIndex.Count - 1; ++i)
                {
                    var currentUserBegin = _userFirstSplitComboIndex[i];
                    var currentUserEnd = _userFirstSplitComboIndex[i + 1];
                    var currentUserNewBegin = compressPointer;

                    //First pass: find max value.
                    var maxValue = 0f;
                    for (int j = currentUserBegin; j < currentUserEnd; ++j)
                    {
                        maxValue = MathF.Max(maxValue, _values[j]);
                    }
                    var removeThreshold = maxValue * valueRatio;

                    //Second pass: compress, removing lower values.
                    for (int j = currentUserBegin; j < currentUserEnd; ++j)
                    {
                        if (_values[j] > removeThreshold)
                        {
                            _values[compressPointer] = _values[j];
                            _splitCombos[compressPointer] = _splitCombos[j];
                            //No need to move _userAdjustmentBuffer because it's updated in each step.

                            compressPointer += 1;
                        }
                    }

                    _userFirstSplitComboIndex[i] = currentUserNewBegin;
                }
                _userFirstSplitComboIndex[^1] = compressPointer;

                _values.RemoveRange(compressPointer, _values.Count - compressPointer);
                _splitCombos.RemoveRange(compressPointer, _splitCombos.Count - compressPointer);
                _userAdjustmentBuffer.RemoveRange(compressPointer, _userAdjustmentBuffer.Count - compressPointer);
            }

            private int CalculateDamage()
            {
                //_bossBuffer used as damage ratio.
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    _bossBuffer[i] = 0;
                }
                for (int i = 0; i < _values.Count; ++i)
                {
                    var value = _values[i];
                    var combo = _splitCombos[i];
                    for (int j = 0; j < 3; ++j)
                    {
                        _bossBuffer[combo.Boss[j]] += combo.Damage[j] * value;
                    }
                }
                bool allPositive = true, allNegative = true;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    if (_bossBuffer[i] < 0.5f && i < _bosses.Count - 1) return -1; //If any boss has damage lower than 50%, it's considered not possible.
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

                for (int i = 0; i < _userFirstSplitComboIndex.Count - 1; ++i) //User
                {
                    var userTotalValue = 0f;
                    var begin = _userFirstSplitComboIndex[i];
                    var end = _userFirstSplitComboIndex[i + 1];
                    for (int j = begin; j < end; ++j)
                    {
                        var comboAdjustment = 0f;
                        var combo = _splitCombos[j];
                        for (int k = 0; k < 3; ++k)
                        {
                            comboAdjustment += _bossBuffer[combo.Boss[k]] * combo.Damage[k];
                        }
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

            private void AdjustGradient(float learningRate)
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

                for (int i = 0; i < _userFirstSplitComboIndex.Count - 1; ++i) //User
                {
                    var userTotalDec = 0f;

                    var begin = _userFirstSplitComboIndex[i];
                    var end = _userFirstSplitComboIndex[i + 1];
                    for (int j = begin; j < end; ++j)
                    {
                        var comboAdjustment = 0f;
                        var combo = _splitCombos[j];
                        for (int k = 0; k < 3; ++k)
                        {
                            comboAdjustment += combo.Damage[k] * _bossBuffer[combo.Boss[k]];
                        }
                        comboAdjustment *= deltaRatio;
                        userTotalDec += comboAdjustment * _values[j];

                        _userAdjustmentBuffer[j] = comboAdjustment;
                    }

                    var userTotalValue = 0f;
                    for (int j = begin; j < end; ++j)
                    {
                        _values[j] += _userAdjustmentBuffer[j] - userTotalDec;
                        if (_values[j] < 0) _values[j] = 0;
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

                result.ComboValues.Clear();

                for (int i = 0; i < _values.Count; ++i)
                {
                    var c = _splitCombos[i];
                    result.ComboValues.TryGetValue(c.ComboID, out var oldValue);
                    result.ComboValues[c.ComboID] = oldValue + _values[i];
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
                SplitCombos();
                InitValues();
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
                SplitCombos();
                InitValues();
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

            public delegate void InitValuesDelegate(StaticInfo staticInfo, int userIndex, List<float> buffer);

            //Run approximately, with given initial values (given by delegate).
            public void RunApproximate(InitValuesDelegate initValues)
            {
                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: false);
                SplitCombos();
                InitValues(initValues);

                float learningRate = 0.01f;
                for (int i = 0; i < 1000; ++i)
                {
                    CalculateDamage();
                    AdjustGradient(learningRate);
                }
            }

            //Initial values from the result of another calculation.
            //If the combo count of a user changes, initalize with same values.
            public static InitValuesDelegate InitValues_DefaultUniform(ResultStorage result)
            {
                return (StaticInfo staticInfo, int userIndex, List<float> buffer) =>
                {
                    var newCount = staticInfo.Users[userIndex].Length;
                    if (result.ComboValues.ContainsKey((userIndex, newCount - 1)) &&
                        !result.ComboValues.ContainsKey((userIndex, newCount)))
                    {
                        //Count matches. Direct copy.
                        for (int i = 0; i < newCount; ++i)
                        {
                            buffer.Add(result.ComboValues[(userIndex, i)]);
                        }
                    }
                    else
                    {
                        //Default: uniform.
                        var avg = 1f / newCount;
                        for (int i = 0; i < newCount; ++i)
                        {
                            buffer.Add(avg);
                        }
                    }
                };
            }

            //Get init values from combo.Value, except for the combos of the last user,
            //whose combos are initialized with uniform values.
            public static void InitValues_FromComboValues(StaticInfo staticInfo, int userIndex, List<float> buffer)
            {
                if (userIndex == staticInfo.Users.Length - 1)
                {
                    var newCount = staticInfo.Users[userIndex].Length;
                    var avg = 1f / newCount;
                    for (int i = 0; i < newCount; ++i)
                    {
                        buffer.Add(avg);
                    }
                }
                else
                {
                    foreach (var c in staticInfo.Users[userIndex])
                    {
                        buffer.Add(c.Value);
                    }
                }
            }

            public static void InitValues_FromComboNetValues(StaticInfo staticInfo, int userIndex, List<float> buffer)
            {
                if (userIndex == staticInfo.Users.Length - 1)
                {
                    var newCount = staticInfo.Users[userIndex].Length;
                    var avg = 1f / newCount;
                    for (int i = 0; i < newCount; ++i)
                    {
                        buffer.Add(avg);
                    }
                }
                else
                {
                    foreach (var c in staticInfo.Users[userIndex])
                    {
                        buffer.Add(c.NetValue);
                    }
                }
            }
        }

        //Run for all users in the guild.
        public static async Task RunAllAsync(ApplicationDbContext context, Guild guild, IEnumerable<UserCombo> allCombos)
        {
            await context.GuildBossStatuses
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == false)
                .DeleteFromQueryAsync();

            allCombos ??= await context.UserCombos
                .Include(c => c.User)
                .Where(c => c.GuildID == guild.GuildID && !c.User.IsIgnored)
                .ToListAsync();
            var allUsers = allCombos
                .GroupBy(c => c.UserID)
                .Select(g => g.ToArray())
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

            var staticInfo = new StaticInfo()
            {
                FirstLapForStages = stageLapList,
                Bosses = bossList,
                BossPlanRatios = planRatios,
                Users = allUsers,
                Guild = guild,
            };

            //Accurate.
            var nonselectedResult = Run(staticInfo, true);

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
                for (int comboIndex = 0; comboIndex < user.Length; ++ comboIndex)
                {
                    var comboID = (userIndex, comboIndex);
                    var c = user[comboIndex];
                    if (totalResult.ComboValues.TryGetValue(comboID, out var netValue))
                    {
                        c.NetValue = netValue;
                    }
                    else
                    {
                        c.NetValue = 0;
                    }
                    if (nonselectedResult.ComboValues.TryGetValue(comboID, out var value))
                    {
                        c.Value = value;
                    }
                    else
                    {
                        c.Value = 0;
                    }
                }
            }
            foreach (var (bossID, value) in nonselectedResult.BossValues)
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
            guild.PredictBossIndex = nonselectedResult.EndBossIndex;
            guild.PredictBossDamageRatio = nonselectedResult.EndBossDamage;
            guild.LastCalculation = TimeZoneHelper.BeijingNow;
        }

        //Run for a single user and get approximate results. This is useful to show some number quickly after
        //the user refreshes the combo list.
        public static async Task RunSingleAsync(ApplicationDbContext context, Guild guild,
            PcrIdentityUser user, List<UserCombo> userCombos)
        {
            var allCombosExcludeOne = await context.UserCombos
                .Include(c => c.User)
                .Where(c => c.GuildID == guild.GuildID && c.User.Id != user.Id)
                .ToListAsync();

            var allUsers = allCombosExcludeOne
                .GroupBy(c => c.UserID)
                .Select(g => g.ToArray())
                .Concat(Enumerable.Repeat(userCombos.ToArray(), 1)) //Put the calculated user last.
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

            var nonselectedResult = RunApproximate(staticInfo, true, guild, Solver.InitValues_FromComboValues);
            var totalResult = RunApproximate(staticInfo, false, guild, Solver.InitValues_FromComboNetValues);

            for (int comboIndex = 0; comboIndex < userCombos.Count; ++comboIndex)
            {
                var comboID = (allUsers.Length - 1, comboIndex);
                var c = userCombos[comboIndex];
                if (totalResult.ComboValues.TryGetValue(comboID, out var netValue))
                {
                    c.NetValue = netValue;
                }
                else
                {
                    c.NetValue = 0;
                }
                if (nonselectedResult.ComboValues.TryGetValue(comboID, out var value))
                {
                    c.Value = value;
                }
                else
                {
                    c.Value = 0;
                }
            }
        }

        public static ResultStorage Run(StaticInfo staticInfo, bool fixSelected)
        {
            var solver = new Solver
            {
                StaticInfo = staticInfo,
                FixSelectedCombo = fixSelected,
            };

            var result = new ResultStorage();
            solver.DamageScale = 1f;

            //Close the current lap.
            var firstBoss = staticInfo.ConvertBossIndex(staticInfo.Guild.BossIndex);
            var totalPower = 1f;
            if (staticInfo.Guild.BossDamageRatio != 0 || firstBoss.Step != 0)
            {
                var lastBossStep = staticInfo.Bosses[firstBoss.Stage].Count - 1;
                var lastBoss = new BossIndexInfo(firstBoss.Stage, firstBoss.Lap, lastBossStep);

                solver.FirstBoss = firstBoss;
                solver.FirstBossHp = staticInfo.Guild.BossDamageRatio;
                solver.LastBoss = lastBoss;

                var avgDamageRatio = solver.RunEstimate();
                if (avgDamageRatio < 1)
                {
                    do
                    {
                        //Try one less.
                        lastBoss = new(lastBoss.Stage, lastBoss.Lap, lastBoss.Step - 1);
                        if (lastBoss.Step < firstBoss.Step)
                        {
                            break;
                        }
                        solver.LastBoss = lastBoss;
                        avgDamageRatio = solver.RunEstimate();
                    } while (avgDamageRatio < 1);
                    solver.Merge(result);
                    if (solver.LastBoss.Step == solver.FirstBoss.Step)
                    {
                        result.EndBossDamage += solver.FirstBossHp;
                        if (result.EndBossDamage > 1)
                        {
                            result.EndBossDamage = 1;
                        }
                    }
                    return result;
                }
                totalPower -= 1f / avgDamageRatio;
                firstBoss = staticInfo.ConvertBossIndex(staticInfo.Guild.BossIndex + (lastBossStep - firstBoss.Step) + 1);
                solver.FirstBossHp = 0;
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
            solver.FirstBossHp = staticInfo.Guild.BossDamageRatio;

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
            var solver = new Solver
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
