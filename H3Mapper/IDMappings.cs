using System;
using System.Collections.Generic;
using System.Linq;
using H3Mapper.Flags;
using Serilog;

namespace H3Mapper
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

        private string TryGetValueForId(int id, IdMap mapping, string name)
        {
            if (mapping.IsEmpty) // to avoid spamming the logs
                return null;

            if (mapping.TryGetValue(id, out var value) == false)
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
            creatureGenerators1.SetCurrentSpecific(format);
            creatureGenerators4.SetCurrentSpecific(format);
            templates.SetCurrentSpecific(format);
        }

        public class TemplateMap
        {
            private ISet<MapObjectTemplate> @default;
            private ISet<MapObjectTemplate>[] valueChain;

            private readonly IDictionary<MapFormat, ISet<MapObjectTemplate>> specific =
                new Dictionary<MapFormat, ISet<MapObjectTemplate>>();

            public void AddFormatMapping(MapFormat format, MapObjectTemplate[] values)
            {
                specific.Add(format, BuildMapping(values));
            }

            private ISet<MapObjectTemplate> BuildMapping(MapObjectTemplate[] values)
            {
                var result = new HashSet<MapObjectTemplate>(values.Length, MapObjectTemplateComparer);
                foreach (var template in values)
                {
                    if (result.Add(template) == false)
                    {
                        throw new ArgumentException($"Duplicate key for element {template}");
                    }
                }

                return result;
            }

            public void AddDefault(MapObjectTemplate[] values)
            {
                @default = BuildMapping(values);
            }

            public bool Contains(MapObjectTemplate value)
            {
                foreach (var collection in valueChain)
                {
                    
                    if (collection.Contains(value))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void SetCurrentSpecific(MapFormat format)
            {
                var chain = new List<ISet<MapObjectTemplate>>(specific.Count + 1);
                var currentFormat = format;
                do
                {
                    if (specific.TryGetValue(currentFormat, out var currentValue))
                    {
                        chain.Add(currentValue);
                    }

                    currentFormat = GetPreviousFormatInChain(currentFormat);
                } while (currentFormat != 0);

                // always at least have the default
                chain.Add(@default);
                valueChain = chain.ToArray();
            }

            private MapFormat GetPreviousFormatInChain(MapFormat format)
            {
                switch (format)
                {
                    case MapFormat.HotA:
                    case MapFormat.WoG:
                        return MapFormat.SoD;
                    case MapFormat.SoD:
                        return MapFormat.AB;
                    case MapFormat.AB:
                        return MapFormat.RoE;
                    default:
                        return 0;
                }
            }
            
            private sealed class MapObjectTemplateEqualityComparer : IEqualityComparer<MapObjectTemplate>
            {
                public bool Equals(MapObjectTemplate x, MapObjectTemplate y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return string.Equals(x.AnimationFile, y.AnimationFile, StringComparison.InvariantCultureIgnoreCase) && x.SupportedTerrainTypes == y.SupportedTerrainTypes && x.Id == y.Id && x.SubId == y.SubId && x.Type == y.Type && x.EditorMenuLocation == y.EditorMenuLocation && x.BlockPosition.Equals(y.BlockPosition) && x.VisitPosition.Equals(y.VisitPosition) && x.IsBackground == y.IsBackground;
                }

                public int GetHashCode(MapObjectTemplate obj)
                {
                    unchecked
                    {
                        var hashCode = StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.AnimationFile);
                        hashCode = (hashCode * 397) ^ (int) obj.SupportedTerrainTypes;
                        hashCode = (hashCode * 397) ^ (int) obj.Id;
                        hashCode = (hashCode * 397) ^ obj.SubId;
                        hashCode = (hashCode * 397) ^ (int) obj.Type;
                        hashCode = (hashCode * 397) ^ (int) obj.EditorMenuLocation;
                        hashCode = (hashCode * 397) ^ obj.BlockPosition.GetHashCode();
                        hashCode = (hashCode * 397) ^ obj.VisitPosition.GetHashCode();
                        hashCode = (hashCode * 397) ^ obj.IsBackground.GetHashCode();
                        return hashCode;
                    }
                }
            }

            public static IEqualityComparer<MapObjectTemplate> MapObjectTemplateComparer { get; } = new MapObjectTemplateEqualityComparer();

        }

        public class IdMap
        {
            private readonly IDictionary<int, string> @default;

            private readonly IDictionary<MapFormat, IDictionary<int, string>> specific =
                new Dictionary<MapFormat, IDictionary<int, string>>();

            private IDictionary<int, string>[] valueChain;

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

                    return specific.Any(s => s.Value.Count > 0) == false;
                }
            }

            public void AddFormatMapping(MapFormat format, IDictionary<int, string> mapping)
            {
                specific.Add(format, mapping);
            }

            public void SetCurrentSpecific(MapFormat format)
            {
                var chain = new List<IDictionary<int, string>>(specific.Count + 1);
                var currentFormat = format;
                do
                {
                    if (specific.TryGetValue(currentFormat, out var currentValue))
                    {
                        chain.Add(currentValue);
                    }

                    currentFormat = GetPreviousFormatInChain(currentFormat);
                } while (currentFormat != 0);

                // always at least have the default
                chain.Add(@default);
                valueChain = chain.ToArray();
            }

            private MapFormat GetPreviousFormatInChain(MapFormat format)
            {
                switch (format)
                {
                    case MapFormat.HotA:
                    case MapFormat.WoG:
                        return MapFormat.SoD;
                    case MapFormat.SoD:
                        return MapFormat.AB;
                    case MapFormat.AB:
                        return MapFormat.RoE;
                    default:
                        return 0;
                }
            }

            public bool TryGetValue(int id, out string value)
            {
                foreach (var collection in valueChain)
                {
                    if (collection.TryGetValue(id, out value))
                    {
                        return true;
                    }
                }

                value = null;
                return false;
            }
        }
    }
}