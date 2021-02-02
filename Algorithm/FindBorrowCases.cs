﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    internal class FindBorrowCases
    {
        private readonly HashSet<int> tempSet = new();
        private readonly List<int> tempList = new();
        private static readonly Dictionary<int, int> _dummyC = new()
        {
            { -2, 0 },
            { -3, 1 },
            { -4, 2 },
            { -5, 3 },
            { -6, 4 },
        };

        public string Run(Dictionary<int, int> a, Dictionary<int, int> b, Dictionary<int, int> c,
            int? fixedA, int? fixedB, int? fixedC)
        {
            try
            {
                if (c is null)
                {
                    if (b is null)
                    {
                        return $"{(fixedA ?? -1)},-1,-1";
                    }
                    return Run3(a, b, _dummyC, fixedA, fixedB, null);
                }
                return Run3(a, b, c, fixedA, fixedB, fixedC);
            }
            catch
            {
                //This should not happen, but the field is required. Let's give it some value anyway.
                return "-1,-1,-1";
            }
        }

        private string Run2(Dictionary<int, int> z1, Dictionary<int, int> z2)
        {
            tempSet.Clear();
            tempSet.UnionWith(z1.Keys);
            tempSet.UnionWith(z2.Keys);
            tempList.Clear();
            tempList.AddRange(z1.Keys);
            tempList.AddRange(z2.Keys);

            if (tempSet.Count == 10)
            {
                //x x x x x
                //x x x x x
                return "-1,-1,-1";
            }
            else
            {
                foreach (var c in tempSet)
                {
                    tempList.Remove(c);
                }
                if (tempList.Count == 1)
                {
                    //A x x x x
                    //A x x x x
                    var a1 = z1[tempList[0]];
                    var a2 = z2[tempList[0]];
                    return $"{a1},-1,-1;-1,{a2},-1";
                }
                else
                {
                    //A B x x x
                    //A B x x x
                    var a1 = z1[tempList[0]];
                    var a2 = z2[tempList[0]];
                    var b1 = z1[tempList[1]];
                    var b2 = z2[tempList[1]];
                    return $"{a1},{b2},-1;{b1},{a2},-1";
                }
            }
        }

        private List<(int key, int count)> _abc = new();
        private (int oldIndex, int[] data)[] _zmatrix = new[] { (0, new int[3]), (0, new int[3]), (0, new int[3]) };
        private List<(int, int, int)> _results = new();
        private HashSet<(int, int, int)> _fixedResults = new();
        private StringBuilder _str = new();

        private static int ZMatrixRowComparison((int oldIndex, int[] data) a, (int oldIndex, int[] data) b)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (a.data[i] == -1)
                {
                    if (b.data[i] == -1)
                    {
                        return 0;
                    }
                    return 1; //Put b first and a after b (a > b).
                }
                else
                {
                    if (b.data[i] == -1)
                    {
                        return -1;
                    }
                    continue;
                }
            }
            return 0;
        }

        private string Run3(Dictionary<int, int> z1, Dictionary<int, int> z2, Dictionary<int, int> z3,
            int? fixedA, int? fixedB, int? fixedC)
        {
            tempSet.Clear();
            tempSet.UnionWith(z1.Keys);
            tempSet.UnionWith(z2.Keys);
            tempSet.UnionWith(z3.Keys);
            tempList.Clear();
            tempList.AddRange(z1.Keys);
            tempList.AddRange(z2.Keys);
            tempList.AddRange(z3.Keys);
            foreach (var x in tempSet)
            {
                tempList.Remove(x);
            }

            //abc is a list containing all ids that will be borrowed.
            _abc.Clear();
            foreach (var i in tempList)
            {
                var abcIndex = _abc.FindIndex(p => p.key == i);
                if (abcIndex == -1)
                {
                    _abc.Add((i, 1));
                }
                else
                {
                    _abc[abcIndex] = (i, _abc[abcIndex].count + 1);
                }
            }
            _abc.Sort((g1, g2) => g1.count - g2.count);
            var abcCount = _abc.Count;
            while (_abc.Count < 3)
            {
                _abc.Add((-1, 0));
            }

            //zmatrix is a matrix to get the CharacterIndex from zindex and abc.
            _zmatrix[0].oldIndex = 0;
            _zmatrix[0].data[0] = z1.TryGetValue(_abc[0].key, out var tmp) ? tmp : -1;
            _zmatrix[0].data[1] = z1.TryGetValue(_abc[1].key, out tmp) ? tmp : -1;
            _zmatrix[0].data[2] = z1.TryGetValue(_abc[2].key, out tmp) ? tmp : -1;
            _zmatrix[1].oldIndex = 1;
            _zmatrix[1].data[0] = z2.TryGetValue(_abc[0].key, out tmp) ? tmp : -1;
            _zmatrix[1].data[1] = z2.TryGetValue(_abc[1].key, out tmp) ? tmp : -1;
            _zmatrix[1].data[2] = z2.TryGetValue(_abc[2].key, out tmp) ? tmp : -1;
            _zmatrix[2].oldIndex = 2;
            _zmatrix[2].data[0] = z3.TryGetValue(_abc[0].key, out tmp) ? tmp : -1;
            _zmatrix[2].data[1] = z3.TryGetValue(_abc[1].key, out tmp) ? tmp : -1;
            _zmatrix[2].data[2] = z3.TryGetValue(_abc[2].key, out tmp) ? tmp : -1;
            Array.Sort(_zmatrix, ZMatrixRowComparison);

            _results.Clear();
            switch ((tempList.Count, abcCount))
            {
            case (3, 3):
                if (_zmatrix[0].data[2] == -1) //ABx AxC xBC
                {
                    _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[2], _zmatrix[2].data[1])); //ACB
                    _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], _zmatrix[2].data[2])); //BAC
                }
                else
                {
                    if (_zmatrix[1].data[1] != -1)
                    {
                        //Case 1: ABC ABx xxC
                        _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[1], _zmatrix[2].data[2])); //ABC
                        _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], _zmatrix[2].data[2])); //BAC
                    }
                    else if (_zmatrix[1].data[2] != -1)
                    {
                        //Case 2: ABC AxC xBx
                        _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[2], _zmatrix[2].data[1])); //ACB
                        _results.Add((_zmatrix[0].data[2], _zmatrix[1].data[0], _zmatrix[2].data[1])); //CAB
                    }
                    else
                    {
                        //Case 3: ABC Axx xBC
                        _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], _zmatrix[2].data[2])); //BAC
                        _results.Add((_zmatrix[0].data[2], _zmatrix[1].data[0], _zmatrix[2].data[1])); //CAB
                    }
                }
                break;
            case (3, 2):
                if (_zmatrix[2].data[0] != -1) //AB AB Ax
                {
                    _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[1], _zmatrix[2].data[0])); //ABA
                    _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], _zmatrix[2].data[0])); //BAA
                }
                else //AB AB xB
                {
                    _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[1], _zmatrix[2].data[1])); //ABB
                    _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], _zmatrix[2].data[1])); //BAB
                }
                break;
            case (2, 2):
                if (_zmatrix[1].data[1] != -1) //AB AB xx
                {
                    _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[1], -1)); //ABx
                    _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], -1)); //BAx
                }
                else //AB Ax xB
                {
                    _results.Add((_zmatrix[0].data[0],                  -1, _zmatrix[2].data[1])); //AxB
                    _results.Add((_zmatrix[0].data[1], _zmatrix[1].data[0], -1));                  //BAx
                    _results.Add((                 -1, _zmatrix[1].data[0], _zmatrix[2].data[1])); //xAB
                }
                break;
            case (2, 1): //A A A
                _results.Add((_zmatrix[0].data[0], _zmatrix[1].data[0], -1));                  //AAx
                _results.Add((_zmatrix[0].data[0],                  -1, _zmatrix[2].data[0])); //AxA
                _results.Add((                 -1, _zmatrix[1].data[0], _zmatrix[2].data[0])); //xAA
                break;
            case (1, 1): //A A x
                _results.Add((_zmatrix[0].data[0], -1, -1)); //Axx
                _results.Add((-1, _zmatrix[1].data[0], -1)); //xAx
                break;
            default:
                _results.Add((-1, -1, -1));
                break;
            }

            //Revert the order from zmatrix[0,1,2] to z1,z2,z3. Apply fixed values.
            _fixedResults.Clear();
            int[] exchangeBuffer = _zmatrix[0].data; //Temporarily use one row in zmatrix for exchanging.
            for (int index = 0; index < _results.Count; ++index)
            {
                var (i, j, k) = _results[index];
                exchangeBuffer[_zmatrix[0].oldIndex] = i;
                exchangeBuffer[_zmatrix[1].oldIndex] = j;
                exchangeBuffer[_zmatrix[2].oldIndex] = k;

                if (fixedA.HasValue && exchangeBuffer[0] != fixedA)
                {
                    if (exchangeBuffer[0] == -1)
                    {
                        exchangeBuffer[0] = fixedA.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (fixedB.HasValue && exchangeBuffer[1] != fixedB)
                {
                    if (exchangeBuffer[1] == -1)
                    {
                        exchangeBuffer[1] = fixedB.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (fixedC.HasValue && exchangeBuffer[2] != fixedC)
                {
                    if (exchangeBuffer[2] == -1)
                    {
                        exchangeBuffer[2] = fixedC.Value;
                    }
                    else
                    {
                        continue;
                    }
                }
                //After fixing, there might be duplicated results. Use hash set to remove those.
                _fixedResults.Add((exchangeBuffer[0], exchangeBuffer[1], exchangeBuffer[2]));
            }

            _str.Clear();
            foreach (var (i, j, k) in _fixedResults)
            {
                if (_str.Length != 0)
                {
                    _str.Append(';');
                }
                _str.Append(i);
                _str.Append(',');
                _str.Append(j);
                _str.Append(',');
                _str.Append(k);
            }

            return _str.ToString();
        }
    }
}
