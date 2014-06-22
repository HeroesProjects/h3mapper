using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var map = new H3Map();
            var info = new MapInfo();
            map.Info = info;
            info.Format = s.ReadEnum<MapFormat>();
            if (IsFullySupported(info.Format) == false)
            {
                Log.Warning(
                    "This is a {format} map. This version is not fully supported yet, some information may be wrong.",
                    info.Format);
            }
            if (IsHota(info.Format))
            {
                s.Skip(4);
            }

            info.HasPlayers = s.ReadBool();
            info.Size = s.Read4ByteNumber();
            info.HasSecondLevel = s.ReadBool();
            info.Name = s.ReadString(30);
            info.Description = s.ReadString(3000);
            info.Difficulty = s.ReadEnum<Difficulty>();
            if (info.Format > MapFormat.RoE)
            {
                info.ExperienceLevelLimit = s.Read1ByteNumber(maxValue: 99);
            }
            const int playerCount = 8;
            info.Players = ReadPlayers(s, playerCount, info);
            info.VictoryCondition = ReadVictoryCondition(s, info);
            info.LossCondition = ReadLossCondition(s, info.Size);
            var teamCount = s.Read1ByteNumber(maxValue: 7);
            if (teamCount > 0)
            {
                foreach (var player in info.Players)
                {
                    player.TeamId = s.Read1ByteNumber(maxValue: teamCount - 1);
                }
            }

            var heroes = new MapHeroes();
            map.Heroes = heroes;
            ReadAllowedHeroes(s, heroes, info.Format);
            ReadHeroCustomisations(s, heroes, info.Format);
            s.Skip(31);
            if (IsHota(info.Format))
            {
                info.AllowSpecialWeeks = s.ReadBool();
                s.Skip(3);
            }
            map.AllowedArtifacts = ReadAllowedArtifacts(s, info.Format);

            if (info.Format >= MapFormat.SoD)
            {
                map.AllowedSpells = ReadSpellsFromBitmask(s);
                map.AllowedSecondarySkills = ReadSecondarySkillsFromBitmask(s);
            }
            map.Rumors = ReadRumors(s);
            ReadHeroConfigurationCustomisations(s, heroes, info.Format);
            map.Terrain = ReadTerrain(s, info);
            map.Objects = ReadMapObjects(s, info);
            map.Events = ReadEvents(s, info.Format, false);
            return map;
        }

        private bool IsFullySupported(MapFormat format)
        {
            return format == MapFormat.RoE || format == MapFormat.AB || format == MapFormat.SoD || IsHota(format);
        }

        private MapObject[] ReadMapObjects(MapDeserializer s, MapInfo info)
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
                var position = ReadPosition(s, info.Size);
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
                        mo = ReadMapEvent(s, info.Format);
                        break;
                    case ObjectId.Hero:
                    case ObjectId.RandomHero:
                    case ObjectId.Prison:
                        mo = ReadMapHero(s, info.Format);
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
                        mo = ReadMapMonster(s, info.Format);
                        break;
                    case ObjectId.OceanBottle:
                    case ObjectId.Sign:
                        mo = ReadMessageObject(s);
                        break;
                    case ObjectId.SeerHut:
                        mo = ReadSeerHut(s, info.Format);
                        break;
                    case ObjectId.WitchHut:
                        mo = ReadWitchHut(s, info.Format);
                        break;
                    case ObjectId.Scholar:
                        mo = ReadScholar(s);
                        break;
                    case ObjectId.Garrison:
                    case ObjectId.Garrison2:
                        mo = ReadGarrison(s, info.Format);
                        break;
                    case ObjectId.Artifact:
                    case ObjectId.RandomArtifact:
                    case ObjectId.RandomTreasureArtifact:
                    case ObjectId.RandomMinorArtifact:
                    case ObjectId.RandomMajorArtifact:
                    case ObjectId.RandomRelicArtifact:
                    case ObjectId.SpellScroll:
                        mo = ReadArtifact(s, info.Format, template.Id);
                        break;
                    case ObjectId.RandomResource:
                    case ObjectId.Resource:
                        mo = ReadMapResource(s, info.Format);
                        break;
                    case ObjectId.RandomTown:
                    case ObjectId.Town:
                        mo = ReadTown(s, info.Format);
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
                        mo = ReadPandorasBox(s, info.Format);
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
                        mo = ReadQuest(s, info.Format);
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
            m.PotentialResources = s.ReadEnum<Resources>();
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
            h.Owner = s.ReadEnum<Player>();
            var id = s.Read1ByteNumber();
            if (id != byte.MaxValue)
            {
                h.Id = id;
            }
            else
            {
                // max value is 8, as only 8 heroes can be active on a map
                h.PowerRating = s.Read1ByteNumber(maxValue: 8);
            }
            return h;
        }

        private DwellingObject ReadDwelling(MapDeserializer s, ObjectId id)
        {
            var d = new DwellingObject();
            d.Player = s.ReadEnum<Player>();
            s.Skip(3);
            if (id != ObjectId.RandomDwellingFaction)
            {
                var sameAsCastleId = s.Read4ByteNumberLong();
                if (sameAsCastleId != 0)
                {
                    d.SameAsCastle = sameAsCastleId;
                }
                else
                {
                    d.AllowedFactions = s.ReadEnum<Factions>();
                }
            }
            if (id != ObjectId.RandomDwellingLevel)
            {
                d.MinLevel = s.ReadEnum<UnitLevel>();
                d.MaxLevel = s.ReadEnum<UnitLevel>();
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
            p.MoraleDifference = s.ReadEnum<LuckMoraleModifier>();
            p.LuckDifference = s.ReadEnum<LuckMoraleModifier>();
            p.Resources = ReadResources(s);
            p.PrimarySkills = ReadPrimarySkills(s);
            p.SecondarySkills = ReadSecondarySkills(s, s.Read1ByteNumber(maxValue: 8));
            p.Artifacts = ReadArtifacts(s, format, s.Read1ByteNumber());
            p.Spells = ReadSpells(s, s.Read1ByteNumber());
            p.Monsters = ReadCreatures(s, format, s.Read1ByteNumber(maxValue: 7));
            s.Skip(8);
            return p;
        }

        private MagicShrineObject ReadMagicShrine(MapDeserializer s)
        {
            var m = new MagicShrineObject();
            var spellId = s.Read1ByteNumber();
            if (spellId != byte.MaxValue)
            {
                m.Spell = ids.GetSpell(spellId);
            }
            s.Skip(3);
            return m;
        }

        private PlayerOwnedObject ReadPlayerObject(MapDeserializer s)
        {
            var m = new PlayerOwnedObject();
            m.Owner = s.ReadEnum<Player>();
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
            m.Owner = s.ReadEnum<Player>();
            var hasName = s.ReadBool();
            if (hasName)
            {
                m.Name = s.ReadString(14);
            }
            var hasGarrison = s.ReadBool();
            if (hasGarrison)
            {
                m.Garrison = ReadCreatures(s, format, 7);
            }
            m.GarrisonFormation = s.ReadEnum<Formation>();
            var hasCustomBuildings = s.ReadBool();
            if (hasCustomBuildings)
            {
                m.BuiltBuildingIds = s.ReadBitmask(6);
                m.ForbiddenBuildingIds = s.ReadBitmask(6);
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
                m.Alignment = s.ReadEnum<Player>();
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
                e.Name = s.ReadString(30000);
                e.Message = s.ReadString(30000);
                e.Resources = ReadResources(s);
                e.Players = s.ReadEnum<Players>();
                if (format > MapFormat.AB)
                {
                    e.HumanAffected = s.ReadBool();
                }
                else
                {
                    e.HumanAffected = true;
                }
                e.ComputerAffected = s.ReadBool();
                e.FirstOccurence = s.Read2ByteNumber(maxValue: 672);
                e.RepeatEvery = s.Read1ByteNumber(maxValue: 28);
                s.Skip(17);
                if (forCastle)
                {
                    e.NewBuildings = s.ReadBitmask(6);
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
            g.Owner = s.ReadEnum<Player>();
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


        private ScholarObject ReadScholar(MapDeserializer s)
        {
            var sc = new ScholarObject();
            sc.BonusType = s.ReadEnum<ScholarBonusType>();
            sc.BonusId = s.Read1ByteNumber();
            s.Skip(6);
            return sc;
        }

        private WitchHutObject ReadWitchHut(MapDeserializer s, MapFormat format)
        {
            var h = new WitchHutObject();
            if (format > MapFormat.RoE)
            {
                h.AllowedSkills = s.ReadBitmask(4);
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
            r.Type = s.ReadEnum<RewardType>();
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
                    r.LuckMorale = s.ReadEnum<LuckMoraleModifier>();
                    break;
                case RewardType.Resource:
                    r.Resource = s.ReadEnum<Resource>();
                    r.Value = s.Read2ByteNumber();
                    s.Skip(2);
                    break;
                case RewardType.PrimarySkill:
                    r.SkillType = s.ReadEnum<PrimarySkillType>();
                    r.Value = s.Read1ByteNumber(maxValue: 99);
                    break;
                case RewardType.SecondarySkill:
                    r.SecondarySkill = new SecondarySkill
                    {
                        Type = s.ReadEnum<SecondarySkillType>(),
                        Level = s.ReadEnum<SecondarySkillLevel>()
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

            q.Type = s.ReadEnum<QuestType>();
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
                    var count = s.Read1ByteNumber(maxValue: 8);
                    var artifactIds = new Identifier[count];
                    for (var i = 0; i < artifactIds.Length; i++)
                    {
                        var id = s.Read2ByteNumber();
                        artifactIds[i] = ids.GetArtifact(id);
                    }
                    q.Artifacts = artifactIds;
                    break;
                case QuestType.ReturnWithCreatures:
                    q.Creatues = ReadCreatures(s, format, s.Read1ByteNumber(maxValue: 7));
                    break;
                case QuestType.ReturnWithResources:
                    q.Resources = ReadResources(s);
                    break;
                case QuestType.BeASpecificHero:
                    q.HeroId = s.Read1ByteNumber();
                    break;
                case QuestType.BelongToASpecificPlayer:
                    q.PlayerId = s.ReadEnum<Player>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknkown quest type " + q.Type);
            }
            var deadline = s.Read4ByteNumberLong();
            if (deadline != uint.MaxValue)
            {
                q.Deadline = (int?) deadline;
            }
            q.FirstVisitText = s.ReadString(30000);
            q.NextVisitText = s.ReadString(30000);
            q.CompletedText = s.ReadString(30000);
            return q;
        }

        private MapObject ReadMessageObject(MapDeserializer s)
        {
            var m = new MapObject
            {
                Message = s.ReadString(150)
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
            m.Disposition = s.ReadEnum<Disposition>();
            var hasMessage = s.ReadBool();
            if (hasMessage)
            {
                m.Message = s.ReadString(30000);
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
            h.Owner = s.ReadEnum<Player>();
            h.SubId = s.Read1ByteNumber();
            var hasName = s.ReadBool();
            if (hasName)
            {
                h.Name = s.ReadString(12);
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
                h.Experience = s.Read4ByteNumberLong();
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
            h.ArmyFormationType = s.ReadEnum<Formation>();

            var hasArtifacts = s.ReadBool();
            if (hasArtifacts)
            {
                h.Inventory = ReadHeroInventory(s, format);
            }
            h.PatrolRadius = s.ReadEnum<PatrolRadius>();
            if (format > MapFormat.RoE)
            {
                var hasBio = s.ReadBool();
                if (hasBio)
                {
                    h.Bio = s.ReadString(30000);
                }
                h.Sex = s.ReadEnum<HeroSex>();
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
                var spellId = s.Read1ByteNumber();
                if (spellId != byte.MaxValue)
                {
                    h.Identifiers = new[]
                    {
                        ids.GetSpell(spellId)
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
            e.GainedExperience = s.Read4ByteNumber(0, 999999999);
            e.ManaDifference = s.Read4ByteNumber();
            e.MoraleDifference = s.ReadEnum<LuckMoraleModifier>();
            e.LuckDifference = s.ReadEnum<LuckMoraleModifier>();
            e.Resources = ReadResources(s);
            e.PrimarySkills = ReadPrimarySkills(s);
            e.SecondarySkills = ReadSecondarySkills(s, s.Read1ByteNumber(maxValue: 8));
            e.Artifacts = ReadArtifacts(s, format, s.Read1ByteNumber());
            e.Spells = ReadSpells(s, s.Read1ByteNumber());
            e.Monsters = ReadCreatures(s, format, s.Read1ByteNumber(maxValue: 7));
            s.Skip(8);
            e.CanBeTriggeredByPlayers = s.ReadEnum<Players>();
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
                o.Message = s.ReadString(30000);
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
                o.AnimationFile = s.ReadString(255 /* not really possible to verify buy they are all really short */);
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
                o.SupportedTerrainTypes = s.ReadEnum<Terrains>();
                o.SupportedTerrainTypes2 = s.ReadEnum<Terrains>();
                o.Id = s.ReadEnum<ObjectId>();
                o.SubId = s.Read4ByteNumber();
                o.Type = s.ReadEnum<ObjectType>();
                o.PrintPriority = s.Read1ByteNumber();

                s.Skip(16); //why?
                co[i] = o;
            }
            return co;
        }

        private MapTerrain ReadTerrain(MapDeserializer s, MapInfo info)
        {
            var terrain = new MapTerrain();

            terrain.Ground = ReadTerrainLevel(s, info, 0);
            if (info.HasSecondLevel)
            {
                terrain.Undrground = ReadTerrainLevel(s, info, 1);
            }

            return terrain;
        }

        private MapTile[][] ReadTerrainLevel(MapDeserializer s, MapInfo info, int level)
        {
            var tiles = new MapTile[info.Size][];
            for (var y = 0; y < info.Size; y++)
            {
                var row = new MapTile[info.Size];
                tiles[y] = row;
                for (var x = 0; x < info.Size; x++)
                {
                    var tile = new MapTile(x, y, level)
                    {
                        TerrainType = s.ReadEnum<Terrain>(),
                        TerrainView = (TerrainView) s.Read1ByteNumber(),
                        RiverType = s.ReadEnum<RiverType>(),
                        RiverDirection = (RiverDirection) s.Read1ByteNumber(),
                        RoadType = s.ReadEnum<RoadType>(),
                        RoadDirection = (RoadDirection) s.Read1ByteNumber(),
                        Flags = (TileMirroring) s.Read1ByteNumber() //two eldest bytes - not used
                    };
                    row[x] = tile;
                }
            }
            return tiles;
        }

        private void ReadHeroConfigurationCustomisations(MapDeserializer s, MapHeroes heroes, MapFormat format)
        {
            // is there a way to be smart and detect it instead?
            var heroCount = 156;
            if (IsHota(format))
            {
                heroCount = s.Read4ByteNumber();
            }
            if (format > MapFormat.AB)
            {
                for (var id = 0; id < heroCount; id++)
                {
                    var hasCustomisations = s.ReadBool();
                    if (hasCustomisations == false)
                    {
                        continue;
                    }
                    var hero = heroes.GetHero(ids.GetHero(id));
                    if (hero.Customisations == null)
                    {
                        hero.Customisations = new HeroCustomisations();
                    }
                    var h = hero.Customisations;
                    var hasExperience = s.ReadBool();
                    if (hasExperience)
                    {
                        h.Experience = s.Read4ByteNumber();
                    }
                    var hasSecondarySkills = s.ReadBool();
                    if (hasSecondarySkills)
                    {
                        var skills = ReadSecondarySkills(s, s.Read4ByteNumber());
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
                        h.Bio = s.ReadString(30000);
                    }
                    h.Sex = s.ReadEnum<HeroSex>();
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
        }

        private Identifier[] ReadSpellsFromBitmask(MapDeserializer s)
        {
            var bitmask = s.ReadBitmaskBits(70);
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
                    Type = s.ReadEnum<SecondarySkillType>(),
                    Level = s.ReadEnum<SecondarySkillLevel>()
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
                var value = s.Read1ByteNumber(maxValue: 99);
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
                var number = s.Read1ByteNumber();
                if (number == byte.MaxValue)
                {
                    return null;
                }
                return number;
            }
            var number2 = s.Read2ByteNumber();
            if (number2 == ushort.MaxValue)
            {
                return null;
            }
            return number2;
        }

        private MapRumor[] ReadRumors(MapDeserializer s)
        {
            var count = s.Read4ByteNumber();
            var rumors = new MapRumor[count];
            for (var i = 0; i < count; i++)
            {
                var r = new MapRumor
                {
                    Name = s.ReadString(30000),
                    Value = s.ReadString(300)
                };
                rumors[i] = r;
            }
            return rumors;
        }

        private SecondarySkillType[] ReadSecondarySkillsFromBitmask(MapDeserializer s)
        {
            var bits = s.ReadBitmaskBits(28);
            var skills = new List<SecondarySkillType>();
            for (var i = 0; i < bits.Length; i++)
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
                return s.ReadBitmaskBits(count);
            }

            return s.ReadBitmask(format == MapFormat.AB ? 17 : 18);
        }


        private void ReadHeroCustomisations(MapDeserializer s, MapHeroes heroes, MapFormat format)
        {
            if (format < MapFormat.SoD)
            {
                return;
            }

            var count = s.Read1ByteNumber();
            for (var i = 0; i < count; i++)
            {
                var hero = heroes.GetHero(ids.GetHero(s.Read1ByteNumber()));
                hero.Customisations = new HeroCustomisations
                {
                    PortraitId = s.Read1ByteNumber(),
                    Name = s.ReadString(12),
                    AllowedForPlayers = s.ReadEnum<Players>()
                };
            }
        }

        private void ReadAllowedHeroes(MapDeserializer s, MapHeroes heroes, MapFormat format)
        {
            var byteCount = GetAllowedHeroesByteCount(format);
            var bitmask = s.ReadBitmask(byteCount);
            for (var i = 0; i < bitmask.Length; i++)
            {
                if (bitmask[i])
                {
                    heroes.AddHero(ids.GetHero(i));
                }
            }
            if (format > MapFormat.RoE && IsHota(format) == false)
            {
                var placeholderCount = s.Read4ByteNumber();
                if (placeholderCount > 0)
                {
                    for (var i = 0; i < placeholderCount; i++)
                    {
                        var hero = ids.GetHero(s.Read1ByteNumber());
                        Log.Warning("Found placeholder for {hero}. No idea what to do with it. Ignoring.", hero);
                    }
                }
            }
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

        private LossCondition ReadLossCondition(MapDeserializer s, int mapSize)
        {
            var type = s.ReadEnum<LossConditionType>();
            var lc = new LossCondition {Type = type};
            if (type != LossConditionType.LossStandard)
            {
                switch (type)
                {
                    case LossConditionType.LossCastle:
                    case LossConditionType.LossHero:
                        lc.Position = ReadPosition(s, mapSize);
                        break;
                    case LossConditionType.TimeExpires:
                        lc.Value = s.Read2ByteNumber(2, 7*4*12);
                        break;
                }
            }
            return lc;
        }

        private VictoryCondition ReadVictoryCondition(MapDeserializer s, MapInfo info)
        {
            var type = s.ReadEnum<VictoryConditionType>();
            var vc = new VictoryCondition {Type = type};
            if (type != VictoryConditionType.WinStandard)
            {
                vc.AllowNormalVictory = s.ReadBool();
                vc.AppliesToAI = s.ReadBool();
                switch (type)
                {
                    case VictoryConditionType.Artifact:
                        vc.Identifier = ids.GetArtifact(s.Read1ByteNumber());
                        if (info.Format > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        break;
                    case VictoryConditionType.GatherTroop:
                        vc.Identifier = ids.GetMonster(s.Read1ByteNumber());
                        if (info.Format > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        vc.Value = s.Read4ByteNumber(1, 99999);
                        break;
                    case VictoryConditionType.GatherResource:
                        vc.Resource = s.ReadEnum<Resource>();
                        vc.Value = s.Read4ByteNumber(1, 9999999);
                        break;
                    case VictoryConditionType.BuildCity:
                        vc.Position = ReadPosition(s, info.Size);
                        vc.HallLevel = s.ReadEnum<BuildingLevel>();
                        vc.CastleLevel = s.ReadEnum<BuildingLevel>();
                        break;
                    case VictoryConditionType.BuildGrail:
                        var position = ReadPosition(s, info.Size, true);
                        if (position != MapPosition.Empty)
                        {
                            vc.Position = position;
                        }
                        break;
                    case VictoryConditionType.BeatHero:
                    case VictoryConditionType.CaptureCity:
                    case VictoryConditionType.BeatMonster:
                        vc.Position = ReadPosition(s, info.Size);
                        break;
                    case VictoryConditionType.TransportItem:
                        vc.Identifier = ids.GetArtifact(s.Read1ByteNumber());
                        vc.Position = ReadPosition(s, info.Size);
                        break;
                    case VictoryConditionType.TakeDwellings:
                    case VictoryConditionType.TakeMines:
                        break;
                    case VictoryConditionType.BeatAllMonsters: // HotA
                        Debug.Assert(IsHota(info.Format));
                        break;
                    case VictoryConditionType.Survive: // HotA
                        Debug.Assert(IsHota(info.Format));
                        vc.Value = s.Read4ByteNumber(1, 9999);
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

        private MapPlayer[] ReadPlayers(MapDeserializer s, int playerCount, MapInfo info)
        {
            var players = new MapPlayer[playerCount];
            for (var i = 0; i < playerCount; i++)
            {
                players[i] = ReadPlayer(s, info.Format, info.Size);
            }
            return players;
        }

        private MapPlayer ReadPlayer(MapDeserializer s, MapFormat format, int mapSize)
        {
            var player = new MapPlayer();
            player.CanHumanPlay = s.ReadBool();
            player.CanAIPlay = s.ReadBool();
            player.AITactic = s.ReadEnum<AITactic>();
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
                player.HomeTownPosition = ReadPosition(s, mapSize);
            }
            player.HasRandomHero = s.ReadBool();
            var heroId = s.Read1ByteNumber();
            if (heroId != byte.MaxValue)
            {
                player.MainCustomHero = ids.GetHero(heroId);
                var portraitId = s.Read1ByteNumber();
                if (portraitId != byte.MaxValue)
                {
                    player.MainCustomHeroPortraitId = portraitId;
                }
                player.MainCustomHeroName = s.ReadString(12);
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
                Id = ids.GetHero(s.Read1ByteNumber()),
                Name = s.ReadString(12)
            };
        }

        private MapPosition ReadPosition(MapDeserializer s, int mapSize, bool allowEmpty = false)
        {
            // this is the position of a lower right corner of an element
            // which may be slightly beyond size of the map
            return new MapPosition
            {
                X = s.Read1ByteNumber(maxValue: mapSize + 8, allowEmpty: allowEmpty),
                Y = s.Read1ByteNumber(maxValue: mapSize + 8, allowEmpty: allowEmpty),
                Z = s.Read1ByteNumber(maxValue: 1, allowEmpty: allowEmpty)
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