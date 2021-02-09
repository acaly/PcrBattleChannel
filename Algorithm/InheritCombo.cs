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
            private readonly List<string> _zhouBorrowIndices = new();
            private int? _nextZhouVariantID;

            public int Count => _zhouVariantIDs.Count;
            public int SelectedZhou => _nextZhouVariantID ?? 0;

            public void Add(int zvid, int borrow, bool isSelected)
            {
                if (isSelected)
                {
                    _nextZhouVariantID = _zhouVariantIDs.Count;
                }
                _zhouVariantIDs.Add(zvid);
                _zhouBorrowIndices.Add(borrow.ToString());
            }

            public void AddEnd()
            {
                _zhouBorrowIndices.Add("-1");
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
                var newBorrows = borrow.Split(';');
                int skippedLength = 0;
                for (int i = 0; i < newBorrows.Length; ++i)
                {
                    var borrowCase = newBorrows[i];
                    var borrowIndices = borrowCase.Split(',');
                    //Compare as string values. (Should be faster than int.Parse.)
                    if (_zhouBorrowIndices[zv1Match] == borrowIndices[0] &&
                        _zhouBorrowIndices[zv2Match] == borrowIndices[1] &&
                        _zhouBorrowIndices[zv3Match] == borrowIndices[2])
                    {
                        //Adjust borrow list.
                        borrow = borrow[skippedLength..] +
                            (skippedLength == 0 ? string.Empty : ";" + borrow[0..(skippedLength - 1)]);
                        return true;
                    }
                    skippedLength += borrowCase.Length + 1;
                }
                return false;
            }
        }

        //Helpers to inherit selected combo after refreshing.
        public static async Task<ComboInheritInfo> GetInheritInfo(ApplicationDbContext context, PcrIdentityUser user,
            HashSet<int> usedCharacterIDs)
        {
            var oldCombo = await context.UserCombos
                .Where(c => c.UserID == user.Id && c.SelectedZhou != null)
                .FirstOrDefaultAsync();

            if (oldCombo is null)
            {
                return null;
            }

            var ret = new ComboInheritInfo();

            var borrowInfo = oldCombo.BorrowInfo.Split(';')[0].Split(',');
            if (oldCombo.Zhou1ID.HasValue)
            {
                await FilterAndAddZhouVariant(context, usedCharacterIDs,
                    oldCombo.Zhou1ID.Value, int.Parse(borrowInfo[0]), oldCombo.SelectedZhou == 0, ret);
            }
            if (oldCombo.Zhou2ID.HasValue)
            {
                await FilterAndAddZhouVariant(context, usedCharacterIDs,
                    oldCombo.Zhou2ID.Value, int.Parse(borrowInfo[1]), oldCombo.SelectedZhou == 1, ret);
            }
            if (oldCombo.Zhou3ID.HasValue)
            {
                await FilterAndAddZhouVariant(context, usedCharacterIDs,
                    oldCombo.Zhou3ID.Value, int.Parse(borrowInfo[2]), oldCombo.SelectedZhou == 2, ret);
            }
            ret.AddEnd();

            return ret;
        }

        private static async Task FilterAndAddZhouVariant(ApplicationDbContext context, HashSet<int> usedCharacterIDs,
            int uzvID, int borrow, bool selected, ComboInheritInfo output)
        {
            var zvid = await context.UserZhouVariants
                .Where(uzv => uzv.UserZhouVariantID == uzvID)
                .Select(uzv => uzv.ZhouVariantID)
                .FirstOrDefaultAsync();
            var zhou = await context.ZhouVariants
                .Include(zv => zv.Zhou)
                .Where(zv => zv.ZhouVariantID == zvid)
                .Select(zv => zv.Zhou)
                .FirstOrDefaultAsync();

            if (borrow != 0 && zhou.C1ID.HasValue && usedCharacterIDs.Contains(zhou.C1ID.Value)) return;
            if (borrow != 1 && zhou.C2ID.HasValue && usedCharacterIDs.Contains(zhou.C2ID.Value)) return;
            if (borrow != 2 && zhou.C3ID.HasValue && usedCharacterIDs.Contains(zhou.C3ID.Value)) return;
            if (borrow != 3 && zhou.C4ID.HasValue && usedCharacterIDs.Contains(zhou.C4ID.Value)) return;
            if (borrow != 4 && zhou.C5ID.HasValue && usedCharacterIDs.Contains(zhou.C5ID.Value)) return;
            output.Add(zvid, borrow, selected);
        }
    }
}
