using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class ZhouParser
    {
        private readonly ApplicationDbContext _context;
        private readonly int _guildID;
        private readonly ICharacterAliasProvider _alias;

        private Dictionary<int, CharacterConfig> _defaultConfigs = new();
        private Dictionary<string, CharacterConfig> _characterNames = new();
        private Dictionary<string, CharacterConfig> _allConfigs = new();
        private Dictionary<int, Dictionary<string, CharacterConfig>> _groupedConfigs = new(); //character ID -> config name -> config
        private Dictionary<string, int> _cachedBossNames = new();

        public ZhouParser(ApplicationDbContext context, int guildID, ICharacterAliasProvider alias)
        {
            _context = context;
            _guildID = guildID;
            _alias = alias;
        }

        private static void GroupConfigs(HashSet<string> nameCheck, IEnumerable<CharacterConfig> configs,
            Dictionary<string, CharacterConfig> results)
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
            GroupConfigs(nameCheck, allConfigs, _allConfigs);
            foreach (var c in allConfigs.GroupBy(cc => cc.CharacterID))
            {
                var group = new Dictionary<string, CharacterConfig>();
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

        public Zhou Parse(string str, bool hasName)
        {
            var words = str.Split(new[] { ' ', '\t', ',', '(', ')', '（', '）', '[', ']', '/' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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

            Dictionary<string, CharacterConfig> currentCharacter = null;
            CharacterConfig cc = null;

            //Skip boss and zhou names.
            for (int i = hasName ? 2 : 1; i < words.Length; ++i)
            {
                var word = words[i];

                //A config of current character (highest priority).
                if (currentCharacter?.TryGetValue(word, out cc) ?? false && cc is not null)
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
                //A named config (high priority).
                else if (_allConfigs.TryGetValue(word, out cc))
                {
                    addedCharacters.Add((cc.Character, word));
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
                //Character alias (low priority).
                else if (_alias.TryGet(word, out var internalID) && _defaultConfigs.TryGetValue(internalID, out cc) ||
                    _characterNames.TryGetValue(word, out cc))
                {
                    addedCharacters.Add((cc.Character, word));
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
                else if (addedCharacters.Count == 5 && i == words.Length - 1)
                {
                    retv.Damage = int.Parse(word);
                    break;
                }
                else
                {
                    //Unknown character
                    throw new Exception();
                }
            }

            if (addedCharacters.Count != 5)
            {
                throw new Exception();
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
