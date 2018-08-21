using H3Mapper.Flags;
using H3Mapper.MapModel;
using Serilog;

namespace H3Mapper.DataModel
{
    public class IdMappings
    {
        private string emptyValue = "--";

        private readonly IdMap artifacts;
        private readonly IdMap heroes;

        private readonly IdMap monsters;

        private readonly IdMap spells;
        private readonly IdMap creatureGenerators1;
        private readonly IdMap creatureGenerators4;
        private readonly TemplateMap templates;

        public IdMappings(IdMap heroes, IdMap spells, IdMap artifacts, IdMap monsters, IdMap creatureGenerators1,
            IdMap creatureGenerators4, TemplateMap templates)
        {
            this.heroes = heroes;
            this.spells = spells;
            this.artifacts = artifacts;
            this.monsters = monsters;
            this.creatureGenerators1 = creatureGenerators1;
            this.creatureGenerators4 = creatureGenerators4;
            this.templates = templates;
        }

        public Identifier GetSpell(int spellId)
        {
            return new Identifier
            {
                Value = spellId,
                Name = TryGetValueForId(spellId, spells, "spell")
            };
        }

        public Identifier GetCreatureGenerator1(int creatureGeneratorId)
        {
            return new Identifier
            {
                Value = creatureGeneratorId,
                Name = TryGetValueForId(creatureGeneratorId, creatureGenerators1, "creatureGenerator1")
            };
        }

        public Identifier GetCreatureGenerator4(int creatureGeneratorId)
        {
            return new Identifier
            {
                Value = creatureGeneratorId,
                Name = TryGetValueForId(creatureGeneratorId, creatureGenerators4, "creatureGenerator4")
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

        public MapObjectTemplate[] GetTemplatesMatching(MapObjectTemplate template)
        {
            return templates.GetValues(template);
        }

        private string TryGetValueForId(int id, IdMap mapping, string name)
        {
            if (mapping.IsEmpty) // to avoid spamming the logs
                return null;

            if (mapping.TryGetValue(id, out var value) == false)
            {
                Log.Information("No name for {itemType} {value}", name, id);
            }

            return value != emptyValue ? value : null;
        }

        public void SetCurrentVersion(MapFormat format)
        {
            artifacts.SetCurrentSpecific(format);
            heroes.SetCurrentSpecific(format);
            spells.SetCurrentSpecific(format);
            monsters.SetCurrentSpecific(format);
            creatureGenerators1.SetCurrentSpecific(format);
            creatureGenerators4.SetCurrentSpecific(format);
            templates.SetCurrentSpecific(format);
        }
    }
}