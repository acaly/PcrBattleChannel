using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PcrBattleChannel.Models
{
    public interface ICharacterAliasProvider
    {
        bool TryGet(string key, out int id);
    }

    public class CharacterAliasProvider : ICharacterAliasProvider
    {
        public class Entry
        {
            [Optional]
            public string Name { get; init; }
            [Optional]
            public int InternalId { get; init; }

            //Alternative field names.
            [Optional]
            public string NickNames { get => Name; init => Name = value; }
            [Optional]
            public int Id { get => InternalId; init => InternalId = value; }
        }

        private readonly Dictionary<string, int> _table = new();

        private static Stream OpenTableFile()
        {
            if (File.Exists("CharacterAliasTable.csv"))
            {
                return File.OpenRead("CharacterAliasTable.csv");
            }
            var res = "PcrBattleChannel.Models.CharacterAliasTable.csv";
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
        }

        public CharacterAliasProvider()
        {
            using var stream = OpenTableFile();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = (header, index) => header.ToLower(),
                HeaderValidated = null,
            };

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);
            foreach (var e in csv.GetRecords<Entry>())
            {
                foreach (var n in e.Name.Split(',', ' ', '/'))
                {
                    _table[n] = e.InternalId;
                }
            }
        }

        public bool TryGet(string key, out int id)
        {
            return _table.TryGetValue(key, out id);
        }
    }
}
