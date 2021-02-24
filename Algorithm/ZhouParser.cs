using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public class ZhouParserFactory
    {
        private readonly ICharacterAliasProvider _alias;

        public ZhouParserFactory(ICharacterAliasProvider alias)
        {
            _alias = alias;
        }

        public ZhouParser GetParser(ApplicationDbContext context, int guildID)
        {
            return new(context, guildID, _alias);
        }
    }

    internal class Trie0<T>
    {
        private readonly Dictionary<char, List<(string str, T data)>> _data = new();

        public void Add(string str, T data)
        {
            if (string.IsNullOrEmpty(str)) return; //Let's ignore these.
            if (!_data.TryGetValue(str[0], out var list))
            {
                list = new();
                _data.Add(str[0], list);
            }
            list.Add((str, data));
            list.Sort((a, b) => b.str.Length - a.str.Length); //Order: longer first.
        }

        public bool Remove(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (!_data.TryGetValue(str[0], out var list))
            {
                return false;
            }
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].str == str)
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public bool TryGet(ref ReadOnlySpan<char> input, out string key, out T result)
        {
            if (input.Length == 0)
            {
                key = default;
                result = default;
                return false;
            }
            if (!_data.TryGetValue(input[0], out var list))
            {
                key = default;
                result = default;
                return false;
            }
            foreach (var (str, data) in list)
            {
                if (input.StartsWith(str, StringComparison.InvariantCultureIgnoreCase))
                {
                    input = input[str.Length..];
                    key = str;
                    result = data;
                    return true;
                }
            }
            key = default;
            result = default;
            return false;
        }
    }

    public class ZhouParser
    {
        private readonly ApplicationDbContext _context;
        private readonly int _guildID;
        private readonly ICharacterAliasProvider _alias;

        private Dictionary<int, CharacterConfig> _defaultConfigs = new();
        private Trie0<CharacterConfig> _characterNames = new();
        private Trie0<CharacterConfig> _allConfigs = new();
        private Dictionary<int, Trie0<CharacterConfig>> _groupedConfigs = new(); //character ID -> config name -> config
        private Dictionary<string, int> _cachedBossNames = new();

        public ZhouParser(ApplicationDbContext context, int guildID, ICharacterAliasProvider alias)
        {
            _context = context;
            _guildID = guildID;
            _alias = alias;
        }

        private static void GroupConfigs(HashSet<string> nameCheck, IEnumerable<CharacterConfig> configs,
            Trie0<CharacterConfig> results)
        {
            nameCheck.Clear();
            foreach (var c in configs)
            {
                if (c.Kind == CharacterConfigKind.Default || nameCheck.Contains(c.Name)) continue;
                if (results.Remove(c.Name))
                {
                    nameCheck.Add(c.Name);
                }
                else
                {
                    results.Add(c.Name, c);
                }
            }
        }

        public async Task ReadDatabase()
        {
            var allConfigs = await _context.CharacterConfigs
                .Include(c => c.Character)
                .Where(c => c.GuildID == _guildID)
                .ToListAsync();
            var nameCheck = new HashSet<string>();
            GroupConfigs(nameCheck, allConfigs.Where(c => c.Kind == CharacterConfigKind.Others), _allConfigs);
            foreach (var c in allConfigs.GroupBy(cc => cc.CharacterID))
            {
                var group = new Trie0<CharacterConfig>();
                GroupConfigs(nameCheck, c, group);
                _groupedConfigs.Add(c.Key, group);
            }
            var allBosses = await _context.Bosses
                .ToListAsync();
            _cachedBossNames = allBosses.ToDictionary(b => b.ShortName, b => b.BossID);
            foreach (var c in allConfigs)
            {
                if (c.Kind == CharacterConfigKind.Default)
                {
                    _defaultConfigs.Add(c.Character.InternalID, c);
                    _characterNames.Add(c.Character.Name, c);
                }
            }
        }

        private bool CheckStandardConfigName(string str, int cid, ref ReadOnlySpan<char> input, out CharacterConfig config)
        {
            foreach (var (regex, kind) in StandardConfigNames.Values)
            {
                var match = regex.Match(str, str.Length - input.Length);
                if (match.Success)
                {
                    input = input[match.Length..];
                    config = new CharacterConfig
                    {
                        Name = match.Value.ToUpperInvariant(),
                        Kind = kind,
                        GuildID = _guildID,
                        CharacterID = cid,
                    };
                    _context.CharacterConfigs.Add(config);
                    return true;
                }
            }
            config = default;
            return false;
        }

        private static void SkipEmptyAndSeparatorChars(ref ReadOnlySpan<char> str)
        {
            while (str.Length > 0 && (char.IsWhiteSpace(str[0]) || Array.IndexOf(_separators, str[0]) != -1))
            {
                str = str[1..];
            }
        }

        private static readonly char[] _separators = new[]
        {
            ' ', '\t', ',', '(', ')', '（', '）', '[', ']', '/',
        };
        private static readonly char[] _separatorsEndDamage = new[] {
            ' ', '\t', ',', '(', ')', '（', '）', '[', ']', '/',
            'k', 'K', 'w', 'W', 'm', 'M',
        };

        private bool ParseNextCharacter(ref ReadOnlySpan<char> remaining, out string importedName, out CharacterConfig cc)
        {
            var r1 = remaining;
            var r2 = remaining;
            CharacterConfig cc2 = null;
            var stdName = _characterNames.TryGet(ref r1, out var importedName1, out var cc1);
            var alias = _alias.TryGet(ref r2, out var importedName2, out var internalID) &&
                    _defaultConfigs.TryGetValue(internalID, out cc2);
            switch ((stdName, alias))
            {
            case (true, true):
                //If both succeeded, take the longer one (e.g. "狼布丁" preferred over "狼").
                if (r1.Length > r2.Length)
                {
                    remaining = r2;
                    importedName = importedName2;
                    cc = cc2;
                    return true;
                }
                else
                {
                    remaining = r1;
                    importedName = importedName1;
                    cc = cc1;
                    return true;
                }
            case (true, false):
                remaining = r1;
                importedName = importedName1;
                cc = cc1;
                return true;
            case (false, true):
                remaining = r2;
                importedName = importedName2;
                cc = cc2;
                return true;
            default:
                importedName = default;
                cc = default;
                return false;
            }
        }

        public Zhou Parse(string str, bool hasName, bool createConfigs, List<CharacterConfig> newConfigs)
        {
            var words = str.Split(_separators, hasName ? 3 : 2,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (!_cachedBossNames.TryGetValue(words[0], out var bossID))
            {
                throw new Exception($"无法识别Boss名：{words[0]}");
            }
            if (hasName && words.Length < 3)
            {
                throw new Exception($"未指定轴名、角色列表和伤害");
            }
            if (!hasName && words.Length < 2)
            {
                throw new Exception($"未指定角色列表和伤害");
            }

            var retv = new ZhouVariant()
            {
                CharacterConfigs = new List<ZhouVariantCharacterConfig>(),
            };
            var ret = new Zhou
            {
                GuildID = _guildID,
                Name = hasName ? words[1] : null,
                Variants = new List<ZhouVariant>() { retv },
                BossID = _cachedBossNames[words[0]],
            };
            retv.Zhou = ret;

            List<(Character character, string word)> addedCharacters = new();

            Trie0<CharacterConfig> currentCharacter = null;
            CharacterConfig cc = null;

            var remainingParentStr = words[^1]; //Already trimmed.
            ReadOnlySpan<char> remaining = remainingParentStr;

            while (true)
            {
                if (remaining.Length == 0)
                {
                    if (addedCharacters.Count == 5)
                    {
                        throw new Exception("未指定伤害");
                    }
                    else
                    {
                        throw new Exception($"阵容列表的角色数量必须为5：{string.Join(' ', addedCharacters.Select(c => c.character))}");
                    }
                }

                string importedName = null;

                //Config of current character.
                if (currentCharacter?.TryGet(ref remaining, out importedName, out cc) ?? false)
                {
                    if (cc.Kind != CharacterConfigKind.Default)
                    {
                        retv.CharacterConfigs.Add(new ZhouVariantCharacterConfig
                        {
                            ZhouVariant = retv,
                            CharacterConfig = cc,
                            CharacterConfigID = cc.CharacterConfigID,
                            OrGroupIndex = (int)cc.Kind,
                        });
                    }
                }
                //New standard config.
                else if (currentCharacter is not null && createConfigs &&
                    CheckStandardConfigName(remainingParentStr, addedCharacters[^1].character.CharacterID, ref remaining, out cc))
                {
                    currentCharacter.Add(cc.Name, cc); //Add it to dict to avoid creating multiple.
                    newConfigs.Add(cc); //Inform caller we have created a new config (need to call ConfigModel.CheckAndAddRankConfig).

                    retv.CharacterConfigs.Add(new ZhouVariantCharacterConfig
                    {
                        ZhouVariant = retv,
                        CharacterConfig = cc,
                        CharacterConfigID = cc.CharacterConfigID == 0 ? null : cc.CharacterConfigID,
                        OrGroupIndex = (int)cc.Kind,
                    });
                }
                //Named configs.
                else if (_allConfigs.TryGet(ref remaining, out importedName, out cc))
                {
                    addedCharacters.Add((cc.Character, importedName));
                    currentCharacter = _groupedConfigs[cc.CharacterID];

                    if (cc.Kind != CharacterConfigKind.Default)
                    {
                        retv.CharacterConfigs.Add(new ZhouVariantCharacterConfig
                        {
                            ZhouVariant = retv,
                            CharacterConfig = cc,
                            CharacterConfigID = cc.CharacterConfigID,
                            OrGroupIndex = (int)cc.Kind,
                        });
                    }
                }
                //Character alias.
                else if (ParseNextCharacter(ref remaining, out importedName, out cc))
                {
                    addedCharacters.Add((cc.Character, importedName));
                    currentCharacter = _groupedConfigs[cc.CharacterID];

                    if (cc.Kind != CharacterConfigKind.Default)
                    {
                        retv.CharacterConfigs.Add(new ZhouVariantCharacterConfig
                        {
                            ZhouVariant = retv,
                            CharacterConfig = cc,
                            CharacterConfigID = cc.CharacterConfigID,
                            OrGroupIndex = (int)cc.Kind,
                        });
                    }
                }
                //End of character list.
                else if (addedCharacters.Count >= 5)
                {
                    if (addedCharacters.Count > 5)
                    {
                        throw new Exception($"超过5个角色：{string.Join(", ", addedCharacters.Select(c => c.word))}");
                    }
                    var endOfDamage = remaining.IndexOfAny(_separatorsEndDamage);
                    if (!int.TryParse(endOfDamage == -1 ? remaining : remaining[..endOfDamage], out var damage))
                    {
                        throw new Exception($"无法识别轴伤害：{remaining[..endOfDamage].ToString()}");
                    }
                    if (endOfDamage != -1)
                    {
                        var multiplier = remaining[endOfDamage] switch
                        {
                            'm' or 'M' => 1_000_000,
                            'w' or 'W' => 10000,
                            'k' or 'K' => 1000,
                            _ => 1,
                        };
                        damage *= multiplier;
                        remaining = remaining[(endOfDamage + 1)..];
                    }
                    else
                    {
                        remaining = remaining[^0..];
                    }
                    retv.Damage = damage;
                    if (remaining.Length > 0)
                    {
                        ret.Description = remaining.ToString();
                    }
                    break;
                }
                else
                {
                    var nextSegment = remaining.ToString().Split(_separators, 2,
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
                    throw new Exception($"无法识别角色名：{nextSegment}");
                }

                SkipEmptyAndSeparatorChars(ref remaining);
            }
            
            addedCharacters.Sort((a, b) => MathF.Sign(a.character.Range - b.character.Range));
            ret.C1ID = addedCharacters[0].character.CharacterID;
            ret.C2ID = addedCharacters[1].character.CharacterID;
            ret.C3ID = addedCharacters[2].character.CharacterID;
            ret.C4ID = addedCharacters[3].character.CharacterID;
            ret.C5ID = addedCharacters[4].character.CharacterID;
            foreach (var ccx in retv.CharacterConfigs)
            {
                ccx.CharacterIndex = addedCharacters.FindIndex(ci => ci.character == ccx.CharacterConfig.Character);
            }

            if (!hasName)
            {
                ret.Name = $"{words[0]} - {addedCharacters[0].word}{addedCharacters[1].word}{addedCharacters[2].word}{addedCharacters[3].word}{addedCharacters[4].word}";
            }

            return ret;
        }
    }
}
