using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public static class InheritCombo
    {
        public class ComboInheritInfo
        {
            private readonly List<int> _zhouVariantIDs = new();
            private readonly List<int> _zhouBorrowIndices = new();
            private int? _nextZhouVariantID;

            public int Count => _zhouVariantIDs.Count;
            public int SelectedZhou => _nextZhouVariantID ?? 0;

            private readonly InMemoryComboBorrowInfo[] _borrowBuffer = new InMemoryComboBorrowInfo[3];

            public void Add(int zvid, int borrow, bool isSelected)
            {
                if (isSelected)
                {
                    _nextZhouVariantID = _zhouVariantIDs.Count;
                }
                _zhouVariantIDs.Add(zvid);
                _zhouBorrowIndices.Add(borrow);
            }

            public void AddEnd()
            {
                _zhouBorrowIndices.Add(-1);
            }

            //note: borrow can be modifed in place (if matching)
            public bool Match(int? zvid1, int? zvid2, int? zvid3, ref InMemoryComboBorrowInfo[] borrow)
            {
                //Count has been confirmed by FindAllCombos. We only need to check
                //in one direction.

                //Zhous
                int zv1Match = zvid1.HasValue ? _zhouVariantIDs.IndexOf(zvid1.Value) : Count;
                int zv2Match = zvid2.HasValue ? _zhouVariantIDs.IndexOf(zvid2.Value) : Count;
                int zv3Match = zvid3.HasValue ? _zhouVariantIDs.IndexOf(zvid3.Value) : Count;

                if (zv1Match < 0 || zv2Match < 0 || zv3Match < 0)
                {
                    return false;
                }

                //Borrow
                Array.Copy(borrow, _borrowBuffer, 3);

                for (int i = 0; i < borrow[0].Count; ++i)
                {
                    //Compare as string values. (Should be faster than int.Parse.)
                    if (_zhouBorrowIndices[zv1Match] == _borrowBuffer[0].Value &&
                        _zhouBorrowIndices[zv2Match] == _borrowBuffer[1].Value &&
                        _zhouBorrowIndices[zv3Match] == _borrowBuffer[2].Value)
                    {
                        //Adjust borrow list.
                        for (int j = 0; j < 3; ++j)
                        {
                            borrow[j] = _borrowBuffer[j];
                        }
                        return true;
                    }
                    for (int j = 0; j < 3; ++j)
                    {
                        _borrowBuffer[j] = _borrowBuffer[j].MakeSwitched();
                    }
                }
                return false;
            }
        }

        //Helpers to inherit selected combo after refreshing.
        public static ComboInheritInfo GetInheritInfo(InMemoryUser user, HashSet<int> usedCharacterIDs)
        {
            if (user.SelectedComboIndex == -1)
            {
                return null;
            }

            var oldCombo = user.GetCombo(user.SelectedComboIndex);
            var ret = new ComboInheritInfo();

            for (int i = 0; i < user.ComboZhouCount; ++i)
            {
                FilterAndAddZhouVariant(usedCharacterIDs, oldCombo.GetZhouVariant(i), oldCombo.GetZhouBorrow(i).Value,
                    user.SelectedComboZhouIndex == i, ret);
            }
            ret.AddEnd();

            return ret;
        }

        private static void FilterAndAddZhouVariant(HashSet<int> usedCharacterIDs,
            InMemoryZhouVariant zv, int borrow, bool selected, ComboInheritInfo output)
        {
            for (int i = 0; i < 5; ++i)
            {
                if (borrow != i && usedCharacterIDs.Contains(zv.CharacterIDs[i])) return;
            }
            output.Add(zv.ZhouVariantID, borrow, selected);
        }
    }
}
