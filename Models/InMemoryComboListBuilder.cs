using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    internal class InMemoryComboListBuilder
    {
        private InMemoryGuild _guild;
        private readonly Dictionary<(int, int, int), int> _bossMap = new();
        private readonly List<List<(int, InMemoryComboBorrowInfo)>> _comboData = new();
        private int _comboDataCount;

        public int ZhouCount { get; private set; }
        public int GroupCount => _comboDataCount;

        public IEnumerable<(int, InMemoryComboBorrowInfo)> GetGroup(int index)
        {
            return _comboData[index];
        }

        public void Reset(InMemoryGuild guild, int zhouCount)
        {
            _guild = guild;
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
            var bossID = (_guild.GetZhouVariantByIndex(z1)?.BossID ?? 0,
                _guild.GetZhouVariantByIndex(z2)?.BossID ?? 0,
                _guild.GetZhouVariantByIndex(z3)?.BossID ?? 0);
            if (!_bossMap.TryGetValue(bossID, out var index))
            {
                index = _comboDataCount++;
                _bossMap.Add(bossID, index);
                if (_comboData.Count == index)
                {
                    _comboData.Add(new());
                }
            }
            if (ZhouCount > 0) _comboData[index].Add((z1, borrowInfo[0]));
            if (ZhouCount > 1) _comboData[index].Add((z2, borrowInfo[1]));
            if (ZhouCount > 2) _comboData[index].Add((z3, borrowInfo[2]));
        }
    }
}
