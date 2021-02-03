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

        public int ComboZhouCount; //How many zhous per combo.

        //Note that these groups are different from the combo group in value calculation, which
        //considers not only boss, but also damage.
        //startIndex is the index of ComboValues, not ComboList. They differ by a factor of ComboZhouCount.
        //The last element marks the total combo count.
        public List<(string name, int startIndex)> ComboGroups { get; } = new();

        public List<float> ComboValues { get; } = new();
        public List<(int zhou, InMemoryComboBorrowInfo borrow)> ComboList { get; } = new(); //zhou: Index of ZhouVariant in guild.

        public int TotalComboCount => ComboGroups[^1].startIndex;

        public void ClearComboList(int comboZhouCount)
        {
            ComboZhouCount = comboZhouCount;
            ComboGroups.Clear();
            ComboList.Clear();
            ComboValues.Clear();
        }

        public InMemoryComboGroupReference GetComboGroup(int index)
        {
            return new InMemoryComboGroupReference
            {
                User = this,
                Index = index,
            };
        }

        public int GetComboGroupCount => ComboGroups.Count - 1;

        public int GetComboCountInGroup(int index)
        {
            return ComboGroups[index + 1].startIndex - ComboGroups[index].startIndex;
        }

        public void RemoveZhouVariant(int index)
        {
            bool ShouldRemoveCombo(int combIndexInUser)
            {
                for (int i = 0; i < ComboZhouCount; ++i)
                {
                    if (ComboList[combIndexInUser * ComboZhouCount + i].zhou == index) return true;
                }
                return false;
            }

            int removedCount = 0;
            for (int groupIndex = 0; groupIndex < ComboGroups.Count - 1; ++groupIndex) //Exclude last.
            {
                var nextGroupStart = ComboGroups[groupIndex + 1].startIndex;

                var g = ComboGroups[groupIndex];
                var oldIndex = g.startIndex;
                var newIndex = oldIndex - removedCount;

                g.startIndex = newIndex;
                ComboGroups[groupIndex] = g;

                for (int comboIndexInUser = oldIndex; comboIndexInUser < nextGroupStart; ++comboIndexInUser)
                {
                    if (!ShouldRemoveCombo(comboIndexInUser))
                    {
                        for (int i = 0; i < ComboZhouCount; ++i)
                        {
                            ComboList[newIndex * ComboZhouCount + i] = ComboList[oldIndex * ComboZhouCount + i];
                        }
                        ComboValues[newIndex] = ComboValues[oldIndex];
                        newIndex += 1;
                    }
                    else
                    {
                        removedCount += 1;
                    }
                    oldIndex += 1;
                }
            }
            ComboList.RemoveRange(ComboList.Count - removedCount * ComboZhouCount, removedCount * ComboZhouCount);
            ComboValues.RemoveRange(ComboValues.Count - removedCount, removedCount);
            ComboGroups[^1] = (null, ComboValues.Count);
        }
    }

    public readonly struct InMemoryComboGroupReference
    {
        public InMemoryUser User { get; init; }
        public int Index { get; init; }

        public string Name => User.ComboGroups[Index].name;
        public int Count => User.GetComboCountInGroup(Index);

        public InMemoryComboReference GetCombo(int index)
        {
            return new InMemoryComboReference
            {
                User = User,
                Index = User.ComboGroups[Index].startIndex + index * User.ComboZhouCount,
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
            return User.Guild.Zhous[User.ComboList[Index * ZhouCount + index].zhou];
        }

        public InMemoryComboBorrowInfo GetZhouBorrow(int index)
        {
            return User.ComboList[Index * ZhouCount + index].borrow;
        }

        public int GetBorrowCaseCount()
        {
            return GetZhouBorrow(0).Count;
        }

        public void SwitchBorrow()
        {
            for (int i = 0; i < ZhouCount; ++i)
            {
                var data = User.ComboList[Index * ZhouCount + i];
                data.borrow = data.borrow.MakeSwitched();
                User.ComboList[Index * ZhouCount + i] = data;
            }
        }
    }
}
