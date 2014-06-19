using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using H3Mapper.Flags;
using H3Mapper.Internal;
using H3Mapper.MapObjects;
using Serilog;

namespace H3Mapper
{
    public class MapReader
    {
        private readonly IDMappings ids;

        public MapReader(IDMappings ids)
        {
            this.ids = ids;
        }

        public H3Map Read(MapDeserializer s)
        {
            var header = new H3Map();

            header.Format = s.Read<MapFormat>();
            if (IsHota(header.Format))
            {
                s.Skip(4);
            }
            header.HasPlayers = s.ReadBool();
            header.Size = s.Read4ByteNumber();
            header.HasSecondLevel = s.ReadBool();
            header.Name = s.ReadString();
            header.Description = s.ReadString();
            header.Difficulty = s.Read<Difficulty>();
            if (header.Format > MapFormat.RoE)
            {
                header.ExperienceLevelLimit = s.Read1ByteNumber();
            }
            const int playerCount = 8;
            header.Players = ReadPlayers(s, playerCount, header.Format);
            header.VictoryCondition = ReadVictoryCondition(s, header.Format);
            header.LossCondition = ReadLossCondition(s);
            header.TeamCount = s.Read1ByteNumber();
            if (header.TeamCount > 0)
            {
                foreach (var player in header.Players)
                {
                    player.TeamId = s.Read1ByteNumber();
                }
            }
            header.AllowedHeroes = ReadAllowedHeroes(s, header.Format);
            header.DisposedHeroes = ReadDisposedHeroes(s, header.Format);
            s.Skip(31);
            if (IsHota(header.Format))
            {
                header.AllowSpecialWeeks = s.ReadBool();
                s.Skip(3);
            }
            header.AllowedArtifacts = ReadAllowedArtifacts(s, header.Format);

            if (header.Format >= MapFormat.SoD)
            {
                header.AllowedSpells = ReadSpellsFromBitmask(s);
                header.AllowedSecondarySkills = ReadSecondarySkillsFromBitmask(s);
            }
            header.Rumors = ReadRumors(s);
            header.PrefedinedHeroes = ReadPredefinedHeroes(s, header.Format);
            header.Terrain = ReadTerrain(s, header);
            header.Objects = ReadMapObjects(s, header.Format);
            header.Events = ReadEvents(s, header.Format, false);
            return header;
        }

        private MapObject[] ReadMapObjects(MapDeserializer s, MapFormat format)
        {
            var templates = ReadMapObjectTemplates(s);
            var count = s.Read4ByteNumberLong();
            if (count == 0)
            {
                return null;
            }
            var objects = new MapObject[count];
            for (var i = 0; i < count; i++)
            {
                var mo = default(MapObject);
                var position = ReadPosition(s);
                var templateIndex = s.Read4ByteNumber();
                if (templateIndex < 0 || templateIndex >= templates.Length)
                {
                    throw new ArgumentOutOfRangeException(string.Format("Map Object at {0} is misaligned.", i));
                }
                var template = templates[templateIndex];
                s.Skip(5); //why?
                switch (template.Id)
                {
                    case ObjectId.Event:
                        mo = ReadMapEvent(s, format);
                        break;
                    case ObjectId.Hero:
                    case ObjectId.RandomHero:
                    case ObjectId.Prison:
                        mo = ReadMapHero(s, format);
                        break;
                    case ObjectId.Monster:
                    case ObjectId.RandomMonster:
                    case ObjectId.RandomMonsterl1:
                    case ObjectId.RandomMonsterl2:
                    case ObjectId.RandomMonsterl3:
                    case ObjectId.RandomMonsterl4:
                    case ObjectId.RandomMonsterl5:
                    case ObjectId.RandomMonsterl6:
                    case ObjectId.RandomMonsterl7:
                        mo = ReadMapMonster(s, format);
                        break;
                    case ObjectId.oceanbottle:
                    case ObjectId.Sign:
                        mo = ReadMessageObject(s);
                        break;
                    case ObjectId.SeerHut:
                        mo = ReadSeerHut(s, format);
                        break;
                    case ObjectId.WitchHut:
                        mo = ReadWitchHut(s, format);
                        break;
                    case ObjectId.Scholar:
                        mo = ReadScholar(s);
                        break;
                    case ObjectId.Garrison:
                    case ObjectId.Garrison2:
                        mo = ReadGarrison(s, format);
                        break;
                    case ObjectId.Artifact:
                    case ObjectId.RandomArtifact:
                    case ObjectId.RandomTreasureArtifact:
                    case ObjectId.RandomMinorArtifact:
                    case ObjectId.RandomMajorArtifact:
                    case ObjectId.RandomRelicArtifact:
                    case ObjectId.SpellScroll:
                        mo = ReadArtifact(s, format, template.Id);
                        break;
                    case ObjectId.RandomResource:
                    case ObjectId.Resource:
                        mo = ReadMapResource(s, format);
                        break;
                    case ObjectId.RandomTown:
                    case ObjectId.Town:
                        mo = ReadTown(s, format);
                        break;
                    case ObjectId.AbandonedMine:
                        mo = ReadAbandonedMine(s);
                        break;
                    case ObjectId.CreatureGenerator1:
                    case ObjectId.CreatureGenerator2:
                    case ObjectId.CreatureGenerator3:
                    case ObjectId.CreatureGenerator4:
                    case ObjectId.Shipyard:
                    case ObjectId.Lighthouse:
                        mo = ReadPlayerObject(s);
                        break;
                    case ObjectId.Mine:
                        if (template.SubId == 7)
                        {
                            goto case ObjectId.AbandonedMine; // SubId == 7 means abandoned mine
                        }
                        mo = ReadPlayerObject(s);
                        break;
                    case ObjectId.ShrineOfMagicGesture:
                    case ObjectId.ShrineOfMagicIncantation:
                    case ObjectId.ShrineOfMagicThought:
                        mo = ReadMagicShrine(s);
                        break;
                    case ObjectId.PandorasBox:
                        mo = ReadPandorasBox(s, format);
                        break;
                    case ObjectId.Grail:
                        mo = ReadGrail(s);
                        break;
                    case ObjectId.RandomDwelling:
                    case ObjectId.RandomDwellingFaction:
                    case ObjectId.RandomDwellingLevel:
                        mo = ReadDwelling(s, template.Id);
                        break;
                    case ObjectId.QuestGuard:
                        mo = ReadQuest(s, format);
                        break;
                    case ObjectId.HeroPlaceholder:
                        mo = ReadHeroPlaceholder(s);
                        break;
                    default:
                        mo = new MapObject(template);
                        break;
                }
                mo.Position = position;
                objects[i] = mo;
            }
            return objects;
        }

        private MapObject ReadAbandonedMine(MapDeserializer s)
        {
            var m = new AbandonedMineObject();
            m.PotentialResources = s.Read<Resources>();
            return m;
        }

        private ResourceObject ReadMapResource(MapDeserializer s, MapFormat format)
        {
            var r = new ResourceObject();
            ReadMessageAndGuards(r, s, format);
            r.Amount = s.Read4ByteNumberLong();
            s.Skip(4);
            return r;
        }

        private HeroPlaceholderObject ReadHeroPlaceholder(MapDeserializer s)
        {
            var h = new HeroPlaceholderObject();
            h.Owner = s.Read<Player>();
            var id = s.ReadNullable(byte.MaxValue);
            if (id.HasValue)
            {
                h.Id = id.Value;
            }
            else
            {
                h.PowerRating = s.Read1ByteNumber(); // max value is 8, as only 8 heroes can be active on a map
            }
            return h;
        }

        private DwellingObject ReadDwelling(MapDeserializer s, ObjectId id)
        {
            var d = new DwellingObject();
            d.Player = s.Read<Player>();
            s.Skip(3);
            if (id != ObjectId.RandomDwellingFaction)
            {
                var sameAsCastleId = s.ReadNullable(0u);
                if (sameAsCastleId.HasValue)
                {
                    d.SameAsCastle = sameAsCastleId.Value;
                }
                else
                {
                    d.AllowedFactions = s.Read<Factions>();
                }
            }
            if (id != ObjectId.RandomDwellingLevel)
            {
                d.MinLevel = s.Read<UnitLevel>();
                d.MaxLevel = s.Read<UnitLevel>();
            }
            return d;
        }

        private GrailObject ReadGrail(MapDeserializer s)
        {
            var g = new GrailObject();
            g.Radius = s.Read4ByteNumber(); // limited to 127
            return g;
        }

        private PandorasBoxObject ReadPandorasBox(MapDeserializer s, MapFormat format)
        {
            var p = new PandorasBoxObject();
            ReadMessageAndGuards(p, s, format);
            p.GainedExperience = s.Read4ByteNumberLong();
            p.ManaDifference = s.Read4ByteNumberLong();
            p.MoraleDifference = s.Read1ByteNumber();
            p.LuckDifference = s.Read1ByteNumber();
            p.Resources = ReadResources(s);
            p.PrimarySkills = ReadPrimarySkills(s);
            p.SecondarySkills = ReadSecondarySkills(s, s.Read1ByteNumber());
            p.Artifacts = ReadArtifacts(s, format, s.Read1ByteNumber());
            p.Spells = ReadSpells(s, s.Read1ByteNumber());
            p.Monsters = ReadCreatures(s, format, s.Read1ByteNumber());
            s.Skip(8);
            return p;
        }

        private MagicShrineObject ReadMagicShrine(MapDeserializer s)
        {
            var m = new MagicShrineObject();
            var spellId = s.ReadNullable(byte.MaxValue);
            if (spellId.HasValue)
            {
                m.Spell = ids.GetSpell(spellId.Value);
            }
            s.Skip(3);
            return m;
        }

        private PlayerOwnedObject ReadPlayerObject(MapDeserializer s)
        {
            var m = new PlayerOwnedObject();
            m.Owner = s.Read<Player>();
            s.Skip(3);
            return m;
        }

        private TownObject ReadTown(MapDeserializer s, MapFormat format)
        {
            var m = new TownObject();
            if (format > MapFormat.RoE)
            {
                m.Identifier = s.Read4ByteNumberLong();
            }
            m.Owner = s.Read<Player>();
            var hasName = s.ReadBool();
            if (hasName)
            {
                m.Name = s.ReadString();
            }
            var hasGarrison = s.ReadBool();
            if (hasGarrison)
            {
                m.Garrison = ReadCreatures(s, format, 7);
            }
            m.GarrisonFormation = s.Read<Formation>();
            var hasCustomBuildings = s.ReadBool();
            if (hasCustomBuildings)
            {
                m.BuiltBuildingIds = ReadBitmask(s, 6, 48);
                m.ForbiddenBuildingIds = ReadBitmask(s, 6, 48);
            }
            else
            {
                m.HasFort = s.ReadBool();
            }
            if (format > MapFormat.RoE)
            {
                m.SpellsWillAppear = ReadSpellsFromBitmask(s);
            }
            m.SpellsMayAppear = ReadSpellsFromBitmask(s);

            m.Events = ReadEvents(s, format, true);
            if (format > MapFormat.AB)
            {
                // this only applies to random castles
                m.Alignment = s.Read<Player>();
            }
            s.Skip(3);
            return m;
        }

        private TimedEvents[] ReadEvents(MapDeserializer s, MapFormat format, bool forCastle)
        {
            var count = s.Read4ByteNumberLong();
            var events = new TimedEvents[count];
            for (var i = 0; i < events.Length; i++)
            {
                var e = new TimedEvents();
                e.Name = s.ReadString();
                e.Message = s.ReadString();
                e.Resources = ReadResources(s);
                e.Players = s.Read<Players>();
                if (format > MapFormat.AB)
                {
                    e.HumanAffected = s.ReadBool();
                }
                else
                {
                    e.HumanAffected = true;
                }
                e.ComputerAffected = s.ReadBool();
                e.FirstOccurence = s.Read2ByteNumber();
                e.RepeatEvery = s.Read1ByteNumber();
                s.Skip(17);
                if (forCastle)
                {
                    e.NewBuildings = ReadBitmask(s, 6);
                    e.Creatures = ReadCastleCreatures(s);
                    s.Skip(4);
                }
            }
            return events;
        }

        private int[] ReadCastleCreatures(MapDeserializer s)
        {
            var creatures = new int[7];
            for (var i = 0; i < creatures.Length; i++)
            {
                creatures[i] = s.Read2ByteNumber();
            }
            return creatures;
        }

        private MapObject ReadArtifact(MapDeserializer s, MapFormat format, ObjectId id)
        {
            var a = id == ObjectId.SpellScroll ? new SpellScrollObject() : new MapObject();
            ReadMessageAndGuards(a, s, format);
            var ss = a as SpellScrollObject;
            if (ss != null)
            {
                ss.Spell = ids.GetSpell(s.Read4ByteNumber());
            }
            return a;
        }

        private GarrisonObject ReadGarrison(MapDeserializer s, MapFormat format)
        {
            var g = new GarrisonObject();
            g.Owner = s.Read<Player>();
            s.Skip(3);
            g.Creatues = ReadCreatures(s, format, 7);
            if (format == MapFormat.RoE)
            {
                g.UnitsAreRemovable = true;
            }
            else
            {
                g.UnitsAreRemovable = s.ReadBool();
            }
            s.Skip(8);
            return g;
        }

        private bool[] ReadBitmask(MapDeserializer s, int byteCount, int? bitCount = null, bool negate = false)
        {
            var bytes = s.Read<BitArray>(byteCount);
            var bits = bytes.OfType<bool>();
            if (bitCount.HasValue)
            {
                bits = bits.Take(bitCount.Value);
            }
            if (negate)
            {
                bits = bits.Select(b => !b);
            }
            return bits.ToArray();
        }

        private ScholarObject ReadScholar(MapDeserializer s)
        {
            var sc = new ScholarObject();
            sc.BonusType = s.Read<ScholarBonusType>();
            sc.BonusId = s.Read1ByteNumber();
            s.Skip(6);
            return sc;
        }

        private WitchHutObject ReadWitchHut(MapDeserializer s, MapFormat format)
        {
            var h = new WitchHutObject();
            if (format > MapFormat.RoE)
            {
                h.AllowedSkills = s.Read<BitArray>(4).OfType<bool>().ToArray();
            }
            return h;
        }

        private SeerHutObject ReadSeerHut(MapDeserializer s, MapFormat format)
        {
            var h = new SeerHutObject();
            h.Quest = ReadQuest(s, format);

            if (h.Quest.Type != QuestType.None)
            {
                h.Reward = ReadReward(s, format);
                s.Skip(2);
            }
            else
            {
                s.Skip(3);
            }
            return h;
        }

        private QuestReward ReadReward(MapDeserializer s, MapFormat format)
        {
            var r = new QuestReward();
            r.Type = s.Read<RewardType>();
            switch (r.Type)
            {
                case RewardType.None:
                    break;
                case RewardType.Experience:
                case RewardType.SpellPoints:
                    r.Value = s.Read4ByteNumberLong();
                    break;
                case RewardType.Morale:
                case RewardType.Luck:
                    r.LuckMorale = s.Read<LuckMoraleModifier>();
                    break;
                case RewardType.Resource:
                    r.Resource = s.Read<Resource>();
                    r.Value = s.Read2ByteNumber();
                    s.Skip(2);
                    break;
                case RewardType.PrimarySkill:
                    r.SkillType = s.Read<PrimarySkillType>();
                    r.Value = s.Read1ByteNumber();
                    break;
                case RewardType.SecondarySkill:
                    r.SecondarySkill = new SecondarySkill
                    {
                        Type = s.Read<SecondarySkillType>(),
                        Level = s.Read<SecondarySkillLevel>()
                    };
                    break;
                case RewardType.Artifact:
                    var itemId = ReadVersionDependantId(s, format).Value;
                    r.Artifact = ids.GetArtifact(itemId);
                    break;
                case RewardType.Spell:
                    r.Spell = ids.GetSpell(s.Read1ByteNumber());
                    break;
                case RewardType.Creatures:
                    r.Monster = ids.GetMonster(ReadVersionDependantId(s, format).Value);
                    r.Value = s.Read2ByteNumber();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknown reward type: " + r.Type);
            }
            return r;
        }

        private QuestObject ReadQuest(MapDeserializer s, MapFormat format)
        {
            var q = new QuestObject();
            if (format == MapFormat.RoE)
            {
                //RoE only supports artifact mission type, with a single artifact
                var artifactId = ReadVersionDependantId(s, format);
                if (artifactId.HasValue)
                {
                    q.Type = QuestType.ReturnWithArtifacts;
                    q.Artifacts = new[] {ids.GetArtifact(artifactId.Value)};
                }
                return q;
            }

            q.Type = s.Read<QuestType>();
            switch (q.Type)
            {
                case QuestType.None:
                    return q;
                case QuestType.AchievePrimarySkillLevel:
                    q.Skills = ReadPrimarySkills(s);
                    break;
                case QuestType.AchieveExperienceLevel:
                    q.Experience = s.Read4ByteNumberLong();
                    break;
                case QuestType.DefeatASpecificHero:
                case QuestType.DefeatASpecificMonster:
                    // NOTE: Position or ID?
                    q.ReferencedId = s.Read4ByteNumberLong();
                    break;
                case QuestType.ReturnWithArtifacts:
                    var count = s.Read1ByteNumber();
                    var artifactIds = new Identifier[count];
                    for (var i = 0; i < artifactIds.Length; i++)
                    {
                        var id = s.Read2ByteNumber();
                        artifactIds[i] = ids.GetArtifact(id);
                    }
                    q.Artifacts = artifactIds;
                    break;
                case QuestType.ReturnWithCreatures:
                    q.Creatues = ReadCreatures(s, format, s.Read1ByteNumber());
                    break;
                case QuestType.ReturnWithResources:
                    q.Resources = ReadResources(s);
                    break;
                case QuestType.BeASpecificHero:
                    q.HeroId = s.Read1ByteNumber();
                    break;
                case QuestType.BelongToASpecificPlayer:
                    q.PlayerId = s.Read<Player>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknkown quest type " + q.Type);
            }
            q.Deadline = (int?) s.ReadNullable(uint.MaxValue);
            q.FirstVisitText = s.ReadString();
            q.NextVisitText = s.ReadString();
            q.CompletedText = s.ReadString();
            return q;
        }

        private MapObject ReadMessageObject(MapDeserializer s)
        {
            var m = new MapObject
            {
                Message = s.ReadString()
            };
            s.Skip(4);
            return m;
        }

        private MonsterObject ReadMapMonster(MapDeserializer s, MapFormat format)
        {
            // NOTE: where does the monster type come from?
            var m = new MonsterObject();
            if (format > MapFormat.RoE)
            {
                m.Identifier = s.Read4ByteNumberLong();
            }
            m.Count = s.Read2ByteNumber();
            m.Disposition = s.Read<Disposition>();
            var hasMessage = s.ReadBool();
            if (hasMessage)
            {
                m.Message = s.ReadString();
                m.Resources = ReadResources(s);
                var artifactId = ReadVersionDependantId(s, format);
                if (artifactId != null)
                {
                    m.Artifact = ids.GetArtifact(artifactId.Value);
                }
            }
            m.AlwaysAttacts = s.ReadBool();
            m.KeepsSize = s.ReadBool();
            s.Skip(2);
            return m;
        }

        private MapObject ReadMapHero(MapDeserializer s, MapFormat format)
        {
            var h = new HeroObject();
            if (format > MapFormat.RoE)
            {
                h.Indentifier = s.Read4ByteNumberLong();
            }
            h.Owner = s.Read<Player>();
            h.SubId = s.Read1ByteNumber();
            var hasName = s.ReadBool();
            if (hasName)
            {
                h.Name = s.ReadString();
            }
            if (format > MapFormat.AB)
            {
                var hasExperience = s.ReadBool();
                if (hasExperience)
                {
                    h.Experience = s.Read4ByteNumberLong();
                }
            }
            else
            {
                h.Experience = s.ReadNullable<uint>(0);
            }
            var hasPotrait = s.ReadBool();
            if (hasPotrait)
            {
                h.PortraitId = s.Read1ByteNumber();
            }
            var hasSecondarySkills = s.ReadBool();
            if (hasSecondarySkills)
            {
                var count = s.Read4ByteNumber();
                h.SecondarySkills = ReadSecondarySkills(s, count);
            }
            var hasArmy = s.ReadBool();
            if (hasArmy)
            {
                h.Army = ReadCreatures(s, format, 7);
            }
            h.ArmyFormationType = s.Read<Formation>();

            var hasArtifacts = s.ReadBool();
            if (hasArtifacts)
            {
                h.Inventory = ReadHeroInventory(s, format);
            }
            h.PatrolRadius = s.ReadNullable(byte.MaxValue);
            if (format > MapFormat.RoE)
            {
                var hasBio = s.ReadBool();
                if (hasBio)
                {
                    h.Bio = s.ReadString();
                }
                h.Sex = s.ReadNullable((HeroSex) byte.MaxValue);
            }
            if (format > MapFormat.AB)
            {
                var hasSpells = s.ReadBool();
                if (hasSpells)
                {
                    h.Identifiers = ReadSpellsFromBitmask(s);
                }
            }
            else if (format == MapFormat.AB)
            {
                var spellId = s.ReadNullable(byte.MaxValue);
                if (spellId.HasValue)
                {
                    h.Identifiers = new[]
                    {
                        ids.GetSpell(spellId.Value)
                    };
                }
            }
            if (format > MapFormat.AB)
            {
                var hasCustomPrimarySkills = s.ReadBool();
                if (hasCustomPrimarySkills)
                {
                    h.PrimarySkills = ReadPrimarySkills(s);
                }
            }
            s.Skip(16); //really?
            return h;
        }

        private MapObject ReadMapEvent(MapDeserializer s, MapFormat format)
        {
            var e = new EventObject();
            ReadMessageAndGuards(e, s, format);
            e.GainedExperience = s.Read4ByteNumber();
            e.ManaDifference = s.Read4ByteNumber();
            e.MoraleDifference = s.Read1ByteNumber();
            e.LuckDifference = s.Read1ByteNumber();
            e.Resources = ReadResources(s);
            e.PrimarySkills = ReadPrimarySkills(s);
            e.SecondarySkills = ReadSecondarySkills(s, s.Read1ByteNumber());
            e.Artifacts = ReadArtifacts(s, format, s.Read1ByteNumber());
            e.Spells = ReadSpells(s, s.Read1ByteNumber());
            e.Monsters = ReadCreatures(s, format, s.Read1ByteNumber());
            s.Skip(8);
            e.CanBeTriggeredByPlayers = s.Read<Players>();
            e.CanBeTriggeredByAI = s.ReadBool();
            e.CancelAfterFirstVisit = s.ReadBool();
            s.Skip(4);
            return e;
        }

        private Identifier[] ReadArtifacts(MapDeserializer s, MapFormat format, int artifactCount)
        {
            var artifacts = new Identifier[artifactCount];
            for (var i = 0; i < artifactCount; i++)
            {
                var id = ReadVersionDependantId(s, format).Value;
                artifacts[i] = ids.GetArtifact(id);
            }
            return artifacts;
        }

        private Identifier[] ReadSpells(MapDeserializer s, int spellCount)
        {
            var spells = new Identifier[spellCount];
            for (var i = 0; i < spellCount; i++)
            {
                var spellId = s.Read1ByteNumber();
                spells[i] = ids.GetSpell(spellId);
            }
            return spells;
        }

        private void ReadMessageAndGuards(MapObject o, MapDeserializer s, MapFormat format)
        {
            var hasMessage = s.ReadBool();
            if (hasMessage)
            {
                o.Message = s.ReadString();
                // NOTE: does it belong inside of this if?
                var hasGuards = s.ReadBool();
                if (hasGuards)
                {
                    o.Guards = ReadCreatures(s, format, 7);
                }
                s.Skip(4);
            }
        }

        private MapMonster[] ReadCreatures(MapDeserializer s, MapFormat format, int creatureCount)
        {
            var creatures = new MapMonster[creatureCount];
            for (var i = 0; i < creatureCount; i++)
            {
                var typeId = ReadVersionDependantId(s, format);
                var count = s.Read2ByteNumber();
                if (typeId.HasValue)
                {
                    creatures[i] = new MapMonster
                    {
                        Monster = ids.GetMonster(typeId.Value),
                        Count = count
                    };
                }
            }
            return creatures;
        }


        private IDictionary<Resource, int> ReadResources(MapDeserializer s)
        {
            var resources = new Dictionary<Resource, int>();
            var keys = Enum.GetValues(typeof (Resource));
            foreach (Resource key in keys)
            {
                var value = s.Read4ByteNumber();
                resources.Add(key, value);
            }
            return resources;
        }

        private MapObjectTemplate[] ReadMapObjectTemplates(MapDeserializer s)
        {
            var count = s.Read4ByteNumber();
            if (count == 0)
            {
                return null;
            }
            if (count > 10000)
            {
                throw new ArgumentOutOfRangeException("Count " + count + " looks wrong. Probably there is a bug here.");
            }
            var co = new MapObjectTemplate[count];
            for (var i = 0; i < count; i++)
            {
                var o = new MapObjectTemplate();
                o.AnimationFile = s.ReadString();
                var blockMask = new bool[6];
                var visitMask = new bool[6];
                for (var j = 0; j < blockMask.Length; j++)
                {
                    blockMask[j] = s.ReadBool();
                }
                for (var j = 0; j < visitMask.Length; j++)
                {
                    visitMask[j] = s.ReadBool();
                }
                o.SupportedTerrainTypes = s.Read<Terrains>();
                o.SupportedTerrainTypes2 = s.Read<Terrains>();
                o.Id = s.Read<ObjectId>();
                o.SubId = s.Read4ByteNumber();
                o.Type = s.Read<ObjectType>();
                o.PrintPriority = s.Read1ByteNumber();

                s.Skip(16); //why?
                co[i] = o;
            }
            return co;
        }

        private MapTerrain ReadTerrain(MapDeserializer s, H3Map header)
        {
            var terrain = new MapTerrain();

            terrain.Ground = ReadTerrainLevel(s, header, 0);
            if (header.HasSecondLevel)
            {
                terrain.Undrground = ReadTerrainLevel(s, header, 1);
            }

            return terrain;
        }

        private MapTile[][] ReadTerrainLevel(MapDeserializer s, H3Map header, int level)
        {
            var tiles = new MapTile[header.Size][];
            for (var y = 0; y < header.Size; y++)
            {
                var row = new MapTile[header.Size];
                tiles[y] = row;
                for (var x = 0; x < header.Size; x++)
                {
                    var tile = new MapTile(x, y, level)
                    {
                        TerrainType = s.Read<Terrain>(),
                        TerrainView = (TerrainView) s.Read1ByteNumber(),
                        RiverType = s.Read<RiverType>(),
                        RiverDirection = (RiverDirection) s.Read1ByteNumber(),
                        RoadType = s.Read<RoadType>(),
                        RoadDirection = (RoadDirection) s.Read1ByteNumber(),
                        Flags = (TileMirroring) s.Read1ByteNumber() //two eldest bytes - not used
                    };
                    row[x] = tile;
                }
            }
            return tiles;
        }

        private MapHeroDefinition[] ReadPredefinedHeroes(MapDeserializer s, MapFormat format)
        {
            // is there a way to be smart and detect it instead?
            var heroCount = 156;
            if (IsHota(format))
            {
                heroCount = s.Read4ByteNumber();
            }
            var list = new List<MapHeroDefinition>();
            if (format > MapFormat.AB)
            {
                for (var id = 0; id < heroCount; id++)
                {
                    var isCustom = s.ReadBool();
                    if (isCustom == false)
                    {
                        continue;
                    }
                    var h = new MapHeroDefinition {HeroId = id};
                    list.Add(h);
                    var hasExperience = s.ReadBool();
                    if (hasExperience)
                    {
                        h.Experience = s.Read4ByteNumber();
                    }
                    var hasSecondarySkills = s.ReadBool();
                    if (hasSecondarySkills)
                    {
                        var secondarySkillCount = s.Read4ByteNumber();
                        var skills = ReadSecondarySkills(s, secondarySkillCount);
                        h.SecondarySkills = skills;
                    }
                    var hasAtrifacts = s.ReadBool();
                    if (hasAtrifacts)
                    {
                        h.Inventory = ReadHeroInventory(s, format);
                    }
                    var hasBio = s.ReadBool();
                    if (hasBio)
                    {
                        h.Bio = s.ReadString();
                    }
                    h.Sex = s.Read<HeroSex>();
                    var hasCustomSpells = s.ReadBool();
                    if (hasCustomSpells)
                    {
                        h.Spells = ReadSpellsFromBitmask(s);
                    }
                    var hasPrimarySkills = s.ReadBool();
                    if (hasPrimarySkills)
                    {
                        var primarySkills = ReadPrimarySkills(s);
                        h.PrimarySkills = primarySkills;
                    }
                }
            }
            return list.ToArray();
        }

        private Identifier[] ReadSpellsFromBitmask(MapDeserializer s)
        {
            var bitmask = ReadBitmask(s, 9, 70);
            var spells = new List<Identifier>();
            for (var i = 0; i < bitmask.Length; i++)
            {
                if (bitmask[i])
                {
                    spells.Add(ids.GetSpell(i));
                }
            }
            return spells.ToArray();
        }

        private static SecondarySkill[] ReadSecondarySkills(MapDeserializer s, int secondarySkillCount)
        {
            var skills = new SecondarySkill[secondarySkillCount];
            for (var i = 0; i < secondarySkillCount; i++)
            {
                skills[i] = new SecondarySkill
                {
                    Type = s.Read<SecondarySkillType>(),
                    Level = s.Read<SecondarySkillLevel>()
                };
            }
            return skills;
        }

        private static IDictionary<PrimarySkillType, int> ReadPrimarySkills(MapDeserializer s)
        {
            var primarySkillTypes = Enum.GetValues(typeof (PrimarySkillType));

            var primarySkills = new Dictionary<PrimarySkillType, int>();
            foreach (PrimarySkillType type in primarySkillTypes)
            {
                var value = s.Read1ByteNumber();
                primarySkills.Add(type, value);
            }
            return primarySkills;
        }

        private HeroArtifact[] ReadHeroInventory(MapDeserializer s, MapFormat format)
        {
            const int artifactSlots = 18;
            var artifacts = new List<HeroArtifact>();
            for (var i = 0; i < artifactSlots; i++)
            {
                artifacts.TryAdd(ReadArtifactForSlot(s, format, (ArtifactSlot) i));
            }
            if (format > MapFormat.AB)
            {
                artifacts.TryAdd(ReadArtifactForSlot(s, format, ArtifactSlot.Misc5));
            }
            //bag artifacts
            var bagSize = s.Read2ByteNumber();
            for (var i = 0; i < bagSize; i++)
            {
                artifacts.TryAdd(ReadArtifactForSlot(s, format, ArtifactSlot.Backpack));
            }
            return artifacts.ToArray();
        }

        private HeroArtifact ReadArtifactForSlot(MapDeserializer s, MapFormat format, ArtifactSlot slot)
        {
            var artifactId = ReadVersionDependantId(s, format);
            if (artifactId.HasValue)
            {
                var artifact = new HeroArtifact
                {
                    Artifact = ids.GetArtifact(artifactId.Value),
                    Slot = slot
                };
                return artifact;
            }
            return null;
        }


        private int? ReadVersionDependantId(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE)
            {
                return s.ReadNullable(byte.MaxValue);
            }
            return s.ReadNullable(ushort.MaxValue);
        }

        private MapRumor[] ReadRumors(MapDeserializer s)
        {
            var count = s.Read4ByteNumber();
            var rumors = new MapRumor[count];
            for (var i = 0; i < count; i++)
            {
                var r = new MapRumor
                {
                    Name = s.ReadString(),
                    Value = s.ReadString()
                };
                rumors[i] = r;
            }
            return rumors;
        }

        private SecondarySkillType[] ReadSecondarySkillsFromBitmask(MapDeserializer s)
        {
            var bits = ReadBitmask(s, 4, 28);
            var skills = new List<SecondarySkillType>();
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == false)
                {
                    skills.Add((SecondarySkillType) i);
                }
            }
            return skills.ToArray();
        }

        private Identifier[] ReadAllowedArtifacts(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE) return null;
            var bits = GetAllowedArtifactsBits(s, format);
            var artifacts = new List<Identifier>(bits.Length);
            for (var i = 0; i < bits.Length; i++)
            {
                if (bits[i] == false)
                {
                    artifacts.Add(ids.GetArtifact(i));
                }
            }
            return artifacts.ToArray();
        }

        private bool[] GetAllowedArtifactsBits(MapDeserializer s, MapFormat format)
        {
            if (IsHota(format))
            {
                var count = s.Read1ByteNumber();
                s.Skip(3);
                return ReadBitmask(s, (int) Math.Ceiling(count/8d), count);
            }

            return ReadBitmask(s, format == MapFormat.AB ? 17 : 18);
        }


        private DisposedHero[] ReadDisposedHeroes(MapDeserializer s, MapFormat format)
        {
            DisposedHero[] dh = null;
            if (format >= MapFormat.SoD)
            {
                var count = s.Read1ByteNumber();
                dh = new DisposedHero[count];
                for (var i = 0; i < dh.Length; i++)
                {
                    dh[i] = new DisposedHero
                    {
                        HeroId = s.Read1ByteNumber(),
                        PortraitId = s.Read1ByteNumber(),
                        Name = s.ReadString(),
                        Players = s.Read1ByteNumber()
                    };
                }
            }
            return dh;
        }

        private MapAllowedHeroes ReadAllowedHeroes(MapDeserializer s, MapFormat format)
        {
            var heroes = new MapAllowedHeroes();
            var byteCount = GetAllowedHeroesByteCount(format);
            var bits = s.Read<BitArray>(byteCount);
            heroes.BitMask = bits.OfType<bool>().ToArray();
            if (format > MapFormat.RoE && !IsHota(format))
            {
                var placeholderCount = s.Read4ByteNumber();
                if (placeholderCount > 0)
                {
                    var placeholderHeroes = new int[placeholderCount];
                    for (var i = 0; i < placeholderCount; i++)
                    {
                        placeholderHeroes[i] = s.Read1ByteNumber();
                    }
                    heroes.Placeholders = placeholderHeroes;
                }
            }
            return heroes;
        }

        private static int GetAllowedHeroesByteCount(MapFormat format)
        {
            if (IsHota(format))
            {
                return 31;
            }
            var byteCount = format == MapFormat.RoE ? 16 : 20;
            return byteCount;
        }

        private LossCondition ReadLossCondition(MapDeserializer s)
        {
            var type = s.Read<LossConditionType>();
            var lc = new LossCondition {Type = type};
            if (type != LossConditionType.LossStandard)
            {
                switch (type)
                {
                    case LossConditionType.LossCastle:
                    case LossConditionType.LossHero:
                        lc.Position = ReadPosition(s);
                        break;
                    case LossConditionType.TimeExpires:
                        lc.Value = s.Read2ByteNumber();
                        break;
                }
            }
            return lc;
        }

        private VictoryCondition ReadVictoryCondition(MapDeserializer s, MapFormat mapFormat)
        {
            var type = s.Read<VictoryConditionType>();
            var vc = new VictoryCondition {Type = type};
            if (type != VictoryConditionType.WinStandard)
            {
                vc.AllowNormalVictory = s.ReadBool();
                vc.AppliesToAI = s.ReadBool();
                switch (type)
                {
                    case VictoryConditionType.Artifact:
                        vc.ObjectType = s.Read1ByteNumber();
                        if (mapFormat > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        break;
                    case VictoryConditionType.GatherTroop:
                        vc.ObjectType = s.Read1ByteNumber();
                        if (mapFormat > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        vc.Value = s.Read4ByteNumber();
                        break;
                    case VictoryConditionType.GatherResource:
                        vc.ObjectType = s.Read1ByteNumber();
                        vc.Value = s.Read4ByteNumber();
                        break;
                    case VictoryConditionType.BuildCity:
                        vc.Position = ReadPosition(s);
                        vc.HallLevel = s.Read<BuildingLevel>();
                        vc.CastleLevel = s.Read<BuildingLevel>();
                        break;
                    case VictoryConditionType.BuildGrail:
                        var position = ReadPosition(s);
                        vc.Position = position;
                        if (vc.Position.Z <= 2)
                        {
                            vc.Position = position;
                        }
                        break;
                    case VictoryConditionType.BeatHero:
                    case VictoryConditionType.CaptureCity:
                    case VictoryConditionType.BeatMonster:
                        vc.Position = ReadPosition(s);
                        break;
                    case VictoryConditionType.TransportItem:
                        vc.ObjectType = s.Read1ByteNumber();
                        vc.Position = ReadPosition(s);
                        break;
                    case VictoryConditionType.TakeDwellings:
                    case VictoryConditionType.TakeMines:
                        break;
                    case VictoryConditionType.BeatAllMonsters: // HotA
                        Debug.Assert(IsHota(mapFormat));
                        break;
                    case VictoryConditionType.Survive: // HotA
                        Debug.Assert(IsHota(mapFormat));
                        vc.Value = s.Read4ByteNumber();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            return vc;
        }

        private static bool IsHota(MapFormat mapFormat)
        {
            return mapFormat == MapFormat.HotA1 ||
                   mapFormat == MapFormat.HotA2 ||
                   mapFormat == MapFormat.HotA3;
        }

        private MapPlayer[] ReadPlayers(MapDeserializer s, int playerCount, MapFormat format)
        {
            var players = new MapPlayer[playerCount];
            for (var i = 0; i < playerCount; i++)
            {
                players[i] = ReadPlayer(s, format);
            }
            return players;
        }

        private MapPlayer ReadPlayer(MapDeserializer s, MapFormat format)
        {
            var player = new MapPlayer();
            player.CanHumanPlay = s.ReadBool();
            player.CanAIPlay = s.ReadBool();
            player.AITactic = s.Read<AITactic>();
            if (format > MapFormat.AB)
            {
                player.P7 = s.Read1ByteNumber();
            }
            player.AllowedFactions = Fractions(s, format);
            player.IsFactionRandom = s.ReadBool();
            player.HasHomeTown = s.ReadBool();

            if (player.HasHomeTown)
            {
                if (format != MapFormat.RoE)
                {
                    player.GenerateHeroAtMainTown = s.ReadBool();
                    player.GenerateHero = s.ReadBool();
                }
                player.HomeTownPosition = ReadPosition(s);
            }
            player.HasRandomHero = s.ReadBool();
            player.MainCustomHeroId = s.ReadNullable(byte.MaxValue);
            if (player.MainCustomHeroId.HasValue)
            {
                player.MainCustomHeroPortraitId = s.ReadNullable(byte.MaxValue);
                player.MainCustomHeroName = s.ReadString();
            }
            if (format > MapFormat.RoE)
            {
                player.PowerPlaceholders = s.Read1ByteNumber();
                var heroCount = s.Read4ByteNumber();
                for (var i = 0; i < heroCount; i++)
                {
                    player.AddHero(ReadHero(s));
                }
            }
            return player;
        }

        private HeroInfo ReadHero(MapDeserializer s)
        {
            return new HeroInfo
            {
                Id = s.Read1ByteNumber(),
                Name = s.ReadString()
            };
        }

        private MapPosition ReadPosition(MapDeserializer s)
        {
            return new MapPosition
            {
                X = s.Read1ByteNumber(),
                Y = s.Read1ByteNumber(),
                Z = s.Read1ByteNumber()
            };
        }

        private static Factions Fractions(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE)
            {
                return (Factions) s.Read1ByteNumber();
            }
            return (Factions) s.Read2ByteNumber();
        }
    }
}