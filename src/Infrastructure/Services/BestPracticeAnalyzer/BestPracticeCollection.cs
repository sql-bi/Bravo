namespace Sqlbi.Bravo.Infrastructure.Services.BestPracticeAnalyzer
{
    using Newtonsoft.Json;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class BestPracticeCollection : IRuleDefinition, IReadOnlyList<BestPracticeRule>
    {
        public static readonly BestPracticeCollection Empty = new();

        private BestPracticeCollection()
        {
        }

        [JsonIgnore]
        public bool Internal { get; set; } = false;

        [JsonIgnore]
        public string? Name { get; set; }

        [JsonIgnore]
        public List<BestPracticeRule> Rules { get; } = new List<BestPracticeRule>();

        string? IRuleDefinition.Name => Name;

        IEnumerable<BestPracticeRule> IRuleDefinition.Rules => Rules;

        public int Count => Rules.Count;

        public BestPracticeRule this[int index] => ((IReadOnlyList<BestPracticeRule>)Rules)[index];

        public static BestPracticeCollection CreateFromFile(string path)
        {
            var collection = new BestPracticeCollection
            {
                Name = Path.GetFileName(path),
            };

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var rules = JsonConvert.DeserializeObject<IEnumerable<BestPracticeRule>>(json);

                if (rules != null)
                {
                    // var query = rules.Where((r) => !collection.Rules.Any((rule) => rule.ID.EqualsI(r.ID))); // TODO: Except
                    collection.Rules.AddRange(rules);
                }
            }

            return collection;
        }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(Rules, Formatting.Indented);
        }

        public BestPracticeRule? this[string id] => Rules.FirstOrDefault((rule) => rule.ID.EqualsI(id));

        public bool Contains(string id)
        {
            return Rules.Any((rule) => rule.ID.EqualsI(id));
        }

        public IEnumerator<BestPracticeRule> GetEnumerator() => Rules.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => Rules.GetEnumerator();
    }
}
