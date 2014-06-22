using System.Collections.Generic;
using H3Mapper.Flags;
using Serilog;

namespace H3Mapper
{
    public class IDMappings
    {
        private string emptyValue = "--";

        private readonly IdMap artifacts;
        private readonly IdMap heroes;

        private readonly IdMap monsters;

        private readonly IdMap spells;

        public IDMappings(IdMap heroes, IdMap spells,
            IdMap artifacts, IdMap monsters)
        {
            this.heroes = heroes;
            this.spells = spells;
            this.artifacts = artifacts;
            this.monsters = monsters;
        }

        public Identifier GetSpell(int spellId)
        {
            return new Identifier
            {
                Value = spellId,
                Name = TryGetValueForId(spellId, spells, "spell")
            };
        }

        public Identifier GetHero(int heroId)
        {
            return new Identifier
            {
                Value = heroId,
                Name = TryGetValueForId(heroId, heroes, "hero")
            };
        }

        public Identifier GetMonster(int monsterId)
        {
            return new Identifier
            {
                Value = monsterId,
                Name = TryGetValueForId(monsterId, monsters, "monster")
            };
        }

        public Identifier GetArtifact(int artifactId)
        {
            return new Identifier
            {
                Value = artifactId,
                Name = TryGetValueForId(artifactId, artifacts, "artifact")
            };
        }

        private string TryGetValueForId(int id, IdMap mapping, string name)
        {
            if (mapping.IsEmpty) // to avoid spamming the logs
                return null;
             
            string value;
            if (mapping.TryGetValue(id, out value) == false)
            {
                Log.Information("No name for {itemType} {value}", name, id);
            }
            if (value != emptyValue)
            {
                return value;
            }
            return null;
        }

        public void SetCurrentVersion(MapFormat format)
        {
            artifacts.SetCurrentSpecific(format);
            heroes.SetCurrentSpecific(format);
            spells.SetCurrentSpecific(format);
            monsters.SetCurrentSpecific(format);
        }

        public class IdMap
        {
            private readonly IDictionary<int, string> @default;
            private IDictionary<MapFormat, IDictionary<int, string>> specific;
            private IDictionary<int, string> specificCurrent;

            public IdMap(IDictionary<int, string> @default)
            {
                this.@default = @default;
            }

            public bool IsEmpty
            {
                get
                {
                    if (@default.Count > 0)
                    {
                        return false;
                    }
                    return specificCurrent != null && specificCurrent.Count > 0;
                }
            }

            public void AddFormatMapping(MapFormat format, IDictionary<int, string> mapping)
            {
                if (specific == null)
                {
                    specific = new Dictionary<MapFormat, IDictionary<int, string>>();
                }
                specific.Add(format, mapping);
            }

            public void SetCurrentSpecific(MapFormat format)
            {
                IDictionary<int, string> current;
                if (specific != null && specific.TryGetValue(format, out current))
                {
                    specificCurrent = current;
                }
                else
                {
                    specificCurrent = null;
                }
            }

            public bool TryGetValue(int id, out string value)
            {
                return specificCurrent != null && specificCurrent.TryGetValue(id, out value) ||
                       @default.TryGetValue(id, out value);
            }
        }
    }
}