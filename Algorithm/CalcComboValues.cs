using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    using ComboVariantExpr = System.Linq.Expressions.Expression<Func<UserCombo, UserZhouVariant>>;
    public static class CalcComboValues
    {
        private class ResultStorage
        {
            //0: OK. >0: try more bosses. <0: try less bosses.
            public int Balance { get; set; }

            public int EndBossIndex { get; set; }
            public float EndBossDamage { get; set; }
            public Dictionary<int, float> ComboValues { get; } = new();
            public Dictionary<int, float> BossValues { get; } = new();
        }

        private struct BossIndexInfo
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

        private class StaticInfo
        {
            public List<int> FirstLapForStages { get; init; }
            public List<List<GuildBossStatus>> Bosses { get; init; }
            public Dictionary<int, ZhouVariant> Zhous { get; set; } //with .Zhou loaded
            public Dictionary<int, float> BossPlanRatios { get; init; } //BossID -> plan ratio

            public BossIndexInfo ConvertBossIndex(int bossIndex)
            {
                int stage = 0, lap = 0, iboss = 0;
                while (bossIndex >= iboss + Bosses[stage].Count)
                {
                    iboss += Bosses[stage].Count;
                    lap += 1;
                    if (stage + 1 < FirstLapForStages.Count && lap >= FirstLapForStages[stage + 1])
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
                    for (int i = 0; i < currentRangeStart.Step; ++i)
                    {
                        results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps - 1));
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

                    //Purge last range. This starts from first boss, but may ends in the middle.
                    var bossCount = Bosses[currentRangeStart.Stage].Count;
                    var bossCountLast = lastBoss.Step + 1;
                    var rangeLaps = lastBoss.Lap - currentRangeStart.Lap + 1;

                    //Note that we reversed the two loops in order to put last boss last.
                    for (int i = bossCountLast; i < bossCount; ++i)
                    {
                        results.Add((new(currentRangeStart.Stage, currentRangeStart.Lap, i), rangeLaps - 1));
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
                public int ComboID;
                private fixed int _boss[3];
                private fixed float _damage[3];

                public Span<int> Boss => MemoryMarshal.CreateSpan(ref _boss[0], 3);
                public Span<float> Damage => MemoryMarshal.CreateSpan(ref _damage[0], 3);
            }

            public float AdjustmentCoefficient { get; set; } = 0.1f;

            public StaticInfo StaticInfo { get; set; }
            public List<PcrIdentityUser> Users { get; set; } //with .Combos.ZhouX loaded
            public bool FixSelectedCombo { get; set; }
            
            public BossIndexInfo FirstBoss { get; set; }
            public BossIndexInfo LastBoss { get; set; }
            public float FirstBossHp { get; set; }

            //DamageScale: Scale damage of all Zhous to allow estimating with more
            //than 1 stage. Use 1 for generating final results
            public float DamageScale { get; set; }

            private readonly List<(BossIndexInfo boss, int count)> _bosses = new();
            private readonly List<float> _bossTotalHp = new();
            private readonly List<float> _bossAdjustWeight = new();
            private readonly List<List<int>> _splitBossTable = new();
            private readonly Dictionary<int, int> _splitBossTableIndex = new();

            private readonly List<SplitComboInfo> _splitCombos = new();
            private readonly List<int> _userFirstSplitComboIndex = new();

            private readonly List<float> _values = new();
            private readonly List<float> _bossBuffer = new(); //for both damage ratio and correction factor.

            private readonly Dictionary<int, float> _mergeBossHp = new();

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
                    _bossTotalHp.Add(hp / bossObj.DamageRatio * DamageScale);

                    maxCount = Math.Max(count, maxCount);
                }

                _bossAdjustWeight.Clear();
                foreach (var (_, count) in _bosses)
                {
                    _bossAdjustWeight.Add(count / (float)maxCount * AdjustmentCoefficient);
                }
                if (lastIsSpecial && _bosses[^1].count == 1)
                {
                    _bossAdjustWeight[^1] *= 0.01f; //Lossen constraint for the last boss.
                }
            }

            private void SplitCombos()
            {
                SplitComboInfo buffer = default;
                void SplitCombo(ref SplitComboInfo buffer, UserCombo combo, int index)
                {
                    var zvid = index switch
                    {
                        0 => combo.Zhou1?.ZhouVariantID,
                        1 => combo.Zhou2?.ZhouVariantID,
                        2 => combo.Zhou3?.ZhouVariantID,
                        _ => default,
                    };
                    if (!zvid.HasValue)
                    {
                        for (int i = index; i < 3; ++i)
                        {
                            buffer.Boss[i] = 0;
                            buffer.Damage[i] = 0;
                        }
                        _splitCombos.Add(buffer);
                        return;
                    }
                    var zv = StaticInfo.Zhous[zvid.Value];
                    var bossID = zv.Zhou.BossID;
                    if (!_splitBossTableIndex.TryGetValue(bossID, out var splitBossTableIndex))
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
                            buffer.Damage[index] = zv.Damage / _bossTotalHp[splitBossID];
                            SplitCombo(ref buffer, combo, index + 1);
                        }
                    }
                }
                _splitCombos.Clear();
                _userFirstSplitComboIndex.Clear();
                foreach (var user in Users)
                {
                    _userFirstSplitComboIndex.Add(_splitCombos.Count);
                    if (FixSelectedCombo)
                    {
                        var selected = user.Combos.FirstOrDefault(c => c.SelectedZhou.HasValue);
                        if (selected is not null)
                        {
                            //Only add one combo.
                            buffer.ComboID = selected.UserComboID;
                            SplitCombo(ref buffer, selected, 0);
                            continue;
                        }
                    }
                    foreach (var combo in user.Combos)
                    {
                        buffer.ComboID = combo.UserComboID;
                        SplitCombo(ref buffer, combo, 0);
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
                    if (allPositive && _bossBuffer[i] < 1 && i < _bosses.Count - 1) allPositive = false;
                    if (allNegative && _bossBuffer[i] > 1) allNegative = false;
                }
                return allPositive ? 1 : allNegative ? -1 : 0;
            }

            private void Adjust()
            {
                //_bossBuffer used for adjustment
                var averageRatio = 1f;
                for (int i = 0; i < _bosses.Count - 1; ++i) //skip last
                {
                    averageRatio += _bossBuffer[i];
                }
                averageRatio /= _bosses.Count;

                var adjustmentTarget = (1 * 1 + averageRatio * 2) / 3; //between 1 and average

                var totalAdjustment = 0f;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    var adjustment = (_bossBuffer[i] - adjustmentTarget) * _bossAdjustWeight[i];
                    _bossBuffer[i] = adjustment;
                    totalAdjustment += adjustment;
                }

                totalAdjustment /= _bosses.Count;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    _bossBuffer[i] -= totalAdjustment;
                }

                //TODO in old algorithm there is a normalization step here
                //is that necessary?

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

                result.EndBossDamage = MathF.Min(1, _bossBuffer[_bosses.Count - 1]);
            }

            public void Run(ResultStorage result, bool forceMergeResults)
            {
                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: true);
                SplitCombos();
                InitValues();
                int damage;

                for (int i = 0; i < 10; ++i)
                {
                    for (int j = 0; j < 50; ++j)
                    {
                        CalculateDamage();
                        Adjust();
                    }
                    CalculateDamage();
                    int minIndex = 0, maxIndex = 0;
                    for (int j = 1; j < _bosses.Count - 1; ++j)
                    {
                        if (_bossBuffer[j] < _bossBuffer[minIndex]) minIndex = j;
                        if (_bossBuffer[j] > _bossBuffer[maxIndex]) maxIndex = j;
                    }
                    var minValue = _bossBuffer[minIndex];
                    var maxValue = _bossBuffer[maxIndex];
                    Adjust();
                    CalculateDamage();
                    var minChange = MathF.Abs(_bossBuffer[minIndex] - minValue);
                    var maxChange = MathF.Abs(_bossBuffer[maxIndex] - maxValue);
                    if (minChange < 0.01f && maxChange < 0.01f)
                    {
                        for (int j = 0; j < _bosses.Count; ++j)
                        {
                            _bossAdjustWeight[j] *= 1.3f;
                        }
                    }
                }

                damage = CalculateDamage();

                result.Balance = damage;
                if (damage == 0 || forceMergeResults)
                {
                    Merge(result);
                }
                else
                {
                    result.BossValues.Clear();
                    result.ComboValues.Clear();
                    result.EndBossDamage = 0f;
                }
            }

            public float RunEstimate()
            {
                ListAndSplitBosses(firstIsSpecial: FirstBossHp != 0, lastIsSpecial: false);
                SplitCombos();
                InitValues();
                for (int i = 0; i < 2; ++i)
                {
                    CalculateDamage();
                    Adjust();
                }
                CalculateDamage();

                var total = 0f;
                for (int i = 0; i < _bosses.Count; ++i)
                {
                    total += _bossBuffer[i];
                }
                return total / _bosses.Count;
            }
        }

        private static float coefficient = 0.01f;

        public static async Task Run(ApplicationDbContext context, Guild guild)
        {
            //TODO avoid recalculating static data in second calculation

            await context.GuildBossStatuses
                .Include(s => s.Boss)
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == false)
                .DeleteFromQueryAsync();

            var allUsers = await context.Users
                .Include(u => u.Combos)
                .Where(u => u.GuildID == guild.GuildID)
                .ToListAsync();

            var totalResult = await Run(context, guild, allUsers, false);
            var nonselectedResult = await Run(context, guild, allUsers, true);

            foreach (var user in allUsers)
            {
                foreach (var c in user.Combos)
                {
                    if (totalResult.ComboValues.TryGetValue(c.UserComboID, out var netValue))
                    {
                        c.NetValue = netValue;
                    }
                    else
                    {
                        c.NetValue = 0;
                    }
                    if (nonselectedResult.ComboValues.TryGetValue(c.UserComboID, out var value))
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
        }

        private static async Task<ResultStorage> Run(ApplicationDbContext context, Guild guild,
            List<PcrIdentityUser> allUsers, bool fixSelected)
        {
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

            var zhous = await context.Zhous
                .Include(z => z.Variants)
                .Where(z => z.GuildID == guild.GuildID)
                .ToListAsync();
            var variants = zhous.SelectMany(z => z.Variants).ToDictionary(v => v.ZhouVariantID);

            ComboVariantExpr selectZhou1 = c => c.Zhou1;
            ComboVariantExpr selectZhou2 = c => c.Zhou2;
            ComboVariantExpr selectZhou3 = c => c.Zhou3;
            foreach (var u in allUsers)
            {
                foreach (var combo in u.Combos)
                {
                    await context.Entry(combo).Reference(selectZhou1).LoadAsync();
                    await context.Entry(combo).Reference(selectZhou2).LoadAsync();
                    await context.Entry(combo).Reference(selectZhou3).LoadAsync();
                }
            }

            var staticInfo = new StaticInfo()
            {
                FirstLapForStages = stageLapList,
                Bosses = bossList,
                Zhous = variants,
                BossPlanRatios = planRatios,
            };
            var solver = new Solver
            {
                StaticInfo = staticInfo,
                Users = allUsers,
                FixSelectedCombo = fixSelected,
            };
            solver.AdjustmentCoefficient = coefficient;

            var result = new ResultStorage();
            solver.DamageScale = 1f;

            //Close the current lap.
            var firstBoss = staticInfo.ConvertBossIndex(guild.BossIndex);
            var totalPower = 1f;
            if (guild.BossDamageRatio != 0 || firstBoss.Step != 0)
            {
                var lastBossStep = bossList[firstBoss.Stage].Count - 1;
                var lastBoss = new BossIndexInfo(firstBoss.Stage, firstBoss.Lap, lastBossStep);

                solver.FirstBossHp = guild.BossDamageRatio;
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
                        avgDamageRatio = solver.RunEstimate();
                    } while (avgDamageRatio < 1);
                    solver.Merge(result);
                    return result;
                }
                totalPower -= 1f / avgDamageRatio;
                firstBoss = staticInfo.ConvertBossIndex(guild.BossIndex + (lastBossStep - firstBoss.Step) + 1);
                solver.FirstBossHp = 0;
            }

            //Run full laps.
            for (int stage = firstBoss.Stage; ; ++stage)
            {
                solver.DamageScale = totalPower;
                solver.FirstBoss = firstBoss;
                solver.LastBoss = new(stage, firstBoss.Lap, bossList[firstBoss.Stage].Count - 1);
                var estimatedLaps = solver.RunEstimate();
                var nextStageStart = stage == stageLapList.Count - 1 ? int.MaxValue : stageLapList[stage + 1];
                var numLapsInThisStage = nextStageStart - firstBoss.Lap;
                if (estimatedLaps < numLapsInThisStage)
                {
                    //Can't finish current stage.
                    var actualLaps = (int)MathF.Floor(estimatedLaps);
                    firstBoss = new(stage, firstBoss.Lap + actualLaps, 0);
                    totalPower -= actualLaps / estimatedLaps * totalPower;
                    break;
                }
                else
                {
                    firstBoss = new(stage + 1, stageLapList[stage + 1], 0);
                    totalPower -= numLapsInThisStage / estimatedLaps * totalPower;
                }
            }

            //Now we have a basic estimation. Run with binary search.
            var firstBossIndex = guild.BossIndex;
            var lastBossIndex = staticInfo.ConvertBossIndex(firstBoss); //estimated position.
            var searchStep = 2;
            //var lastBossIndex = firstBossIndex;
            //var searchStep = 1;
            int? lastBalance = null;
            int count = 0;

            do
            {
                solver.FirstBoss = staticInfo.ConvertBossIndex(firstBossIndex);
                solver.LastBoss = staticInfo.ConvertBossIndex(lastBossIndex);
                solver.Run(result, false);
                result.EndBossIndex = lastBossIndex;
                if (result.Balance == 0)
                {
                    return result;
                }
                else
                {
                    if (lastBalance.HasValue && lastBalance != result.Balance)
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
            } while (searchStep > 0 && ++count < 20);

            solver.Merge(result);
            return result;
        }
    }
}
