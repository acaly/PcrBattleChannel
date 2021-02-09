using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public unsafe struct InMemoryComboBorrowInfo
    {
        private fixed sbyte _borrowIndex[4];

        private InMemoryComboBorrowInfo(sbyte a, sbyte b, sbyte c, sbyte count)
        {
            _borrowIndex[0] = a;
            _borrowIndex[1] = b;
            _borrowIndex[2] = c;
            _borrowIndex[3] = count;
        }

        public InMemoryComboBorrowInfo(int a) : this((sbyte)a, 0, 0, 1)
        {
        }

        public readonly int Value => Count == 0 ? -1 : _borrowIndex[0];
        public readonly int Count => _borrowIndex[3];

        public readonly InMemoryComboBorrowInfo MakeSwitched()
        {
            var count = _borrowIndex[3];
            if (count == 0) return this;
            return new InMemoryComboBorrowInfo(_borrowIndex[count - 1], _borrowIndex[0], _borrowIndex[1], count);
        }

        public readonly InMemoryComboBorrowInfo MakeAppend(int a)
        {
            return new InMemoryComboBorrowInfo((sbyte)a, _borrowIndex[0], _borrowIndex[1], (sbyte)(Count + 1));
        }
    }

    //Track a PcrIdentityUser entry in database.
    public class InMemoryUser
    {
        private struct ComboGroupData
        {
            public int StartIndex;
        }

        public InMemoryGuild Guild { get; init; }
        public string UserID { get; init; }
        public int Index { get; init; } //Index of a user is fixed once created.
        public DateTime LastComboCalculation;

        public int ComboZhouCount { get; private set; } //How many zhous per combo.

        //Note that these groups are different from the combo group in value calculation, which
        //considers not only boss, but also damage.
        //startIndex is the index of ComboValues, not ComboList. They differ by a factor of ComboZhouCount.
        //The last element marks the total combo count.
        private readonly List<ComboGroupData> _comboGroups = new() { new() { StartIndex = 0 } };

        private readonly List<(float current, float total)> _comboValues = new();
        private readonly List<(int zhou, InMemoryComboBorrowInfo borrow)> _comboList = new(); //zhou: Index of ZhouVariant in guild.

        public int SelectedComboIndex { get; set; }
        public int SelectedComboZhouIndex { get; set; } //0,1,2

        public int TotalComboCount => _comboGroups[^1].StartIndex;

        public void ClearComboList(int? comboZhouCount)
        {
            if (comboZhouCount.HasValue)
            {
                ComboZhouCount = comboZhouCount.Value;
            }
            SelectedComboIndex = SelectedComboZhouIndex = -1;
            _comboGroups.Clear();
            _comboList.Clear();
            _comboValues.Clear();
            LastComboCalculation = default;
        }

        public ComboGroup GetComboGroup(int index)
        {
            return new ComboGroup
            {
                User = this,
                Index = index,
            };
        }

        public Combo GetCombo(int index)
        {
            return new Combo
            {
                User = this,
                Index = index,
            };
        }

        public int ComboGroupCount => _comboGroups.Count - 1;

        public int GetComboCountInGroup(int index)
        {
            return _comboGroups[index + 1].StartIndex - _comboGroups[index].StartIndex;
        }

        public IEnumerable<Combo> AllCombos
        {
            get => Enumerable.Range(0, TotalComboCount).Select(i => GetCombo(i));
        }

        public void RemoveZhouVariant(int index)
        {
            bool ShouldRemoveCombo(int combIndexInUser)
            {
                for (int i = 0; i < ComboZhouCount; ++i)
                {
                    if (_comboList[combIndexInUser * ComboZhouCount + i].zhou == index) return true;
                }
                return false;
            }

            int removedCount = 0;
            for (int groupIndex = 0; groupIndex < _comboGroups.Count - 1; ++groupIndex) //Exclude last.
            {
                var nextGroupStart = _comboGroups[groupIndex + 1].StartIndex;

                var g = _comboGroups[groupIndex];
                var oldIndex = g.StartIndex;
                var newIndex = oldIndex - removedCount;

                g.StartIndex = newIndex;
                _comboGroups[groupIndex] = g;

                for (int comboIndexInUser = oldIndex; comboIndexInUser < nextGroupStart; ++comboIndexInUser)
                {
                    if (!ShouldRemoveCombo(comboIndexInUser))
                    {
                        for (int i = 0; i < ComboZhouCount; ++i)
                        {
                            _comboList[newIndex * ComboZhouCount + i] = _comboList[oldIndex * ComboZhouCount + i];
                        }
                        _comboValues[newIndex] = _comboValues[oldIndex];
                        newIndex += 1;
                    }
                    else
                    {
                        if (SelectedComboIndex == oldIndex)
                        {
                            SelectedComboIndex = SelectedComboZhouIndex = -1;
                        }
                        removedCount += 1;
                    }
                    oldIndex += 1;
                }
            }
            _comboList.RemoveRange(_comboList.Count - removedCount * ComboZhouCount, removedCount * ComboZhouCount);
            _comboValues.RemoveRange(_comboValues.Count - removedCount, removedCount);
            _comboGroups[^1] = new() { StartIndex = _comboValues.Count };
        }

        public void MatchAllZhouVariants(HashSet<int> userAllCharacterIDs, HashSet<int> userAllConfigIDs)
        {
            ClearComboList(null);

            foreach (var zv in Guild.ZhouVariants)
            {
                int? borrowId = null;
                void SetBorrow(int index)
                {
                    borrowId = borrowId.HasValue ? -1 : index;
                }
                bool CheckCharacterConfig(int index)
                {
                    foreach (var g in zv.CharacterConfigIDs[index])
                    {
                        if (!g.Any(c => userAllConfigIDs.Contains(c)))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                for (int i = 0; i < 5; ++i)
                {
                    if (!userAllCharacterIDs.Contains(zv.CharacterIDs[i]) || !CheckCharacterConfig(i)) SetBorrow(i);
                }
                var borrowPlusOne = (borrowId ?? 5) + 1;
                zv.UserData[Index].BorrowPlusOne = (byte)borrowPlusOne;
            }
        }

        internal void WriteComboList(InMemoryComboListBuilder builder)
        {
            ComboZhouCount = builder.ZhouCount;
            _comboGroups.Clear();
            _comboValues.Clear();
            _comboList.Clear();
            SelectedComboIndex = SelectedComboZhouIndex = -1;

            for (int i = 0; i < builder.GroupCount; ++i)
            {
                _comboGroups.Add(new() { StartIndex = _comboValues.Count });
                _comboList.AddRange(builder.GetGroup(i));
            }
            _comboGroups.Add(new() { StartIndex = _comboValues.Count });
        }

        //Called after WriteComboList.
        internal void InheritSelectedCombo(int[] zhouIndices, InMemoryComboBorrowInfo[] borrowInfo, int selectedZhou)
        {
            for (int i = 0; i < _comboList.Count; i += ComboZhouCount)
            {
                bool match = true;
                for (int j = 0; j < ComboZhouCount; ++j)
                {
                    if (_comboList[i + j].zhou != zhouIndices[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    SelectedComboIndex = i;
                    SelectedComboZhouIndex = selectedZhou;
                    for (int j = 0; j < ComboZhouCount; ++j)
                    {
                        var updateData = _comboList[i + j];
                        updateData.borrow = borrowInfo[j];
                        _comboList[i + j] = updateData;
                    }
                    return;
                }
            }
        }

        public readonly struct ComboGroup
        {
            public InMemoryUser User { get; init; }
            public int Index { get; init; }

            public int Count => User.GetComboCountInGroup(Index);

            public Combo GetCombo(int index)
            {
                return new Combo
                {
                    User = User,
                    Index = User._comboGroups[Index].StartIndex + index * User.ComboZhouCount,
                };
            }
        }

        public readonly struct Combo
        {
            public InMemoryUser User { get; init; }
            public int Index { get; init; }

            public int ZhouCount => User.ComboZhouCount;

            public float CurrentValue
            {
                get => User._comboValues[Index].current;
                set
                {
                    var update = User._comboValues[Index];
                    update.current = value;
                    User._comboValues[Index] = update;
                }
            }

            public float TotalValue
            {
                get => User._comboValues[Index].total;
                set
                {
                    var update = User._comboValues[Index];
                    update.total = value;
                    User._comboValues[Index] = update;
                }
            }

            public InMemoryZhouVariant GetZhouVariant(int index)
            {
                return User.Guild.GetZhouVariantByIndex(User._comboList[Index * ZhouCount + index].zhou);
            }

            public InMemoryComboBorrowInfo GetZhouBorrow(int index)
            {
                return User._comboList[Index * ZhouCount + index].borrow;
            }

            public (int, int, int) BossIDTuple
            {
                get => (GetZhouVariant(0)?.BossID ?? 0, GetZhouVariant(1)?.BossID ?? 0, GetZhouVariant(2)?.BossID ?? 0);
            }

            public Vector3 DamageVector
            {
                get => new(GetZhouVariant(0)?.Damage ?? 0, GetZhouVariant(1)?.Damage ?? 0, GetZhouVariant(2)?.Damage ?? 0);
            }

            public int GetBorrowCaseCount()
            {
                return GetZhouBorrow(0).Count;
            }

            public void SwitchBorrow()
            {
                for (int i = 0; i < ZhouCount; ++i)
                {
                    var data = User._comboList[Index * ZhouCount + i];
                    data.borrow = data.borrow.MakeSwitched();
                    User._comboList[Index * ZhouCount + i] = data;
                }
            }
        }
    }
}
