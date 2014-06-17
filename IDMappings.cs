using System.Collections.Generic;
using Serilog;

namespace H3Mapper
{
    public class IDMappings
    {
        public IDMappings(IDictionary<int, string> heroes, IDictionary<int, string> spells, IDictionary<int, string> artifacts, IDictionary<int, string> monsters)
        {
            this.heroes = heroes;
            this.spells = spells;
            this.artifacts = artifacts;
            this.monsters = monsters;
        }

        private readonly IDictionary<int, string> heroes;

        private readonly IDictionary<int, string> monsters;

        private readonly IDictionary<int, string> spells;

        private readonly IDictionary<int, string> artifacts;

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