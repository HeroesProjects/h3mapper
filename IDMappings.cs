using System.Collections.Generic;
using Serilog;

namespace H3Mapper
{
    public class IDMappings
    {
        public IDictionary<int, string> Heroes { get; set; }

        public IDictionary<int, string> Spells { get; set; }

        public IDictionary<int, string> Artifacts { get; set; }

        public Identifier GetSpell(int spellId)
        {
            return new Identifier
            {
                Value = spellId,
                Name = TryGetValueForId(spellId, Spells, "spell")
            };
        }

        public Identifier GetArtifact(int artifactId)
        {
            return new Identifier
            {
                Value = artifactId,
                Name = TryGetValueForId(artifactId, Artifacts, "artifact")
            };
        }

        private string TryGetValueForId(int id, IDictionary<int, string> mapping, string name)
        {
            string value;
            if (mapping.TryGetValue(id, out value) == false)
            {
                if (mapping.Count > 0)// to avoid spamming the logs
                {
                    Log.Information("No name for {itemType} {value}", name, id);
                }
            }
            return value;
        }
    }
}