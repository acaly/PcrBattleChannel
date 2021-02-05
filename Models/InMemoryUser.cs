using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public unsafe struct InMemoryComboBorrowInfo
    {
        private fixed byte _borrowIndex[4];

        private InMemoryComboBorrowInfo(byte a, byte b, byte c, byte count)
        {
            _borrowIndex[0] = a;
            _borrowIndex[1] = b;
            _borrowIndex[2] = c;
            _borrowIndex[3] = count;
        }

        public readonly int Value => _borrowIndex[0];
        public readonly int Count => _borrowIndex[3];

        public readonly InMemoryComboBorrowInfo MakeSwitched()
        {
            var count = _borrowIndex[3];
            return new InMemoryComboBorrowInfo(_borrowIndex[count - 1], _borrowIndex[0], _borrowIndex[1], count);
        }
    }

    //Track a PcrIdentityUser entry in database.
    public class InMemoryUser
    {
        public InMemoryGuild Guild { get; init; }
        public string UserID { get; init; }
        public int Index { get; init; } //Index of a user is fixed once created.
        public DateTime LastComboCalculation;

        public int ComboZhouCount { get; private set; } //How many zhous per combo.

        //Note that these groups are different from the combo group in value calculation, which
        //considers not only boss, but also damage.
        //startIndex is the index of ComboValues, not ComboList. They differ by a factor of ComboZhouCount.
        //The last element marks the total combo count.
        private readonly List<(string name, int startIndex)> _comboGroups = new() { (null, 0) };

        private readonly List<(float current, float total)> _comboValues = new();
        private readonly List<(int zhou, InMemoryComboBorrowInfo borrow)> _comboList = new(); //zhou: Index of ZhouVariant in guild.

        public int SelectedComboIndex { get; set; }
        public int SelectedComboZhouIndex { get; set; }

        public int TotalComboCount => _comboGroups[^1].startIndex;

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
        }

        public InMemoryComboGroupReference GetComboGroup(int index)
        {
            return new InMemoryComboGroupReference
            {
                User = this,
                Index = index,
            };
        }

        public int GetComboGroupCount => _comboGroups.Count - 1;

        public int GetComboCountInGroup(int index)
        {
            return _comboGroups[index + 1].startIndex - _comboGroups[index].startIndex;
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
                var nextGroupStart = _comboGroups[groupIndex + 1].startIndex;

                var g = _comboGroups[groupIndex];
                var oldIndex = g.startIndex;
                var newIndex = oldIndex - removedCount;

                g.startIndex = newIndex;
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
            _comboGroups[^1] = (null, _comboValues.Count);
        }

        public readonly struct InMemoryComboGroupReference
        {
            public InMemoryUser User { get; init; }
            public int Index { get; init; }

            public string Name => User._comboGroups[Index].name;
            public int Count => User.GetComboCountInGroup(Index);

            public InMemoryComboReference GetCombo(int index)
            {
                return new InMemoryComboReference
                {
                    User = User,
                    Index = User._comboGroups[Index].startIndex + index * User.ComboZhouCount,
                };
            }
        }

        public readonly struct InMemoryComboReference
        {
            public InMemoryUser User { get; init; }
            public int Index { get; init; }

            public int ZhouCount => User.ComboZhouCount;

            public InMemoryZhouVariant GetZhouVariant(int index)
            {
                return User.Guild.GetZhouVariantByIndex(User._comboList[Index * ZhouCount + index].zhou);
            }

            public InMemoryComboBorrowInfo GetZhouBorrow(int index)
            {
                return User._comboList[Index * ZhouCount + index].borrow;
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
