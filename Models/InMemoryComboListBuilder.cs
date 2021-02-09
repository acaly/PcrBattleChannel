using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    internal class InMemoryComboListBuilder
    {
        private readonly Dictionary<(int, int, int), int> _bossMap = new();
        private readonly List<List<(int, InMemoryComboBorrowInfo)>> _comboData = new();
        private int _comboDataCount;

        public int ZhouCount { get; private set; }
        public int GroupCount => _comboDataCount;

        public IEnumerable<(int, InMemoryComboBorrowInfo)> GetGroup(int index)
        {
            return _comboData[index];
        }

        public void Reset(int zhouCount)
        {
            foreach (var list in _comboData)
            {
                list.Clear();
            }
            _bossMap.Clear();
            _comboDataCount = 0;
            ZhouCount = zhouCount;
        }

        public void AddCombo(int z1, int z2, int z3, InMemoryComboBorrowInfo[] borrowInfo)
        {
            var bossID = (z1, z2, z3);
            if (!_bossMap.TryGetValue(bossID, out var index))
            {
                index = ++_comboDataCount;
                if (_comboData.Count == index)
                {
                    _comboData.Add(new());
                }
            }
            if (GroupCount > 0) _comboData[index].Add((z1, borrowInfo[0]));
            if (GroupCount > 1) _comboData[index].Add((z2, borrowInfo[1]));
            if (GroupCount > 2) _comboData[index].Add((z3, borrowInfo[2]));
        }
    }
}
