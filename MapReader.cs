using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace H3Mapper
{
    public class MapReader
    {
        public MapHeader Read(Stream mapFile)
        {
            var s = new MapDeserializer(mapFile);

            var header = new MapHeader();

            header.Format = s.Read<MapFormat>();
            if (IsHota(header.Format))
            {
                s.Skip(4);
            }
            header.HasPlayers = s.Read<bool>();
            header.Size = s.Read<int>();
            header.HasSecondLevel = s.Read<bool>();
            header.Name = s.Read<string>();
            header.Description = s.Read<string>();
            header.Difficulty = s.Read<byte>();
            if (header.Format > MapFormat.RoE)
            {
                header.ExperienceLevelLimit = s.Read<byte>();
            }
            const int playerCount = 8;
            header.Players = ReadPlayers(s, playerCount, header.Format);
            header.VictoryCondition = ReadVictoryCondition(s, header.Format);
            header.LossCondition = ReadLossCondition(s);
            header.TeamCount = s.Read<byte>();
            if (header.TeamCount > 0)
            {
                foreach (var player in header.Players)
                {
                    player.TeamId = s.Read<byte>();
                }
            }
            header.AllowedHeroes = ReadAllowedHeroes(s, header.Format);
            header.DisposedHeroes = ReadDisposedHeroes(s, header.Format);
            header.AllowedArtifacts = ReadAllowedArtifacts(s, header.Format);
            header.AllowedSpellsAndAbilities = ReadAllowedSpellsAndAbilities(s, header.Format);
            if (IsHota(header.Format))
            {
                // something is wrong above, not sure which one... between heroes and 
                s.Skip(11);
            }
            header.Rumors = ReadRumors(s);
            header.PrefedinedHeroes = ReadPredefinedHeroes(s, header.Format);
            header.Terrain = ReadTerrain(s, header);
            header.CustomObjects = ReadCustomObjects(s, header.Format);
            header.Objects = ReadMapObjects(s, header.Format, header.CustomObjects);
            header.Events = ReadEvents(s, header.Format);
            return header;
        }

        private MapObject[] ReadMapObjects(MapDeserializer s, MapFormat format, CustomObject[] templates)
        {
            var count = s.Read<int>();
            if (count == 0)
            {
                return null;
            }
            var objects = new MapObject[count];
            for (var i = 0; i < count; i++)
            {
                var mo = default(MapObject);
                var position = ReadPosition(s);
                var templateIndex = s.Read<int>();
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
                    case ObjectId.RandomTown:
                    case ObjectId.Town:
                        mo = ReadTown(s, format);
                        break;
                    case ObjectId.Mine:
                    case ObjectId.AbandonedMine:
                    case ObjectId.CreatureGenerator1:
                    case ObjectId.CreatureGenerator2:
                    case ObjectId.CreatureGenerator3:
                    case ObjectId.CreatureGenerator4:
                    case ObjectId.Shipyard:
                    case ObjectId.lighthouse:
                        mo = ReadPlayerObject(s);
                        break;
                    case ObjectId.ShrineOfMagicGesture:
                    case ObjectId.ShrineOfMagicIncantation:
                    case ObjectId.ShrineOfMagicThought:
                        mo = ReadMagicShrine(s);
                        break;
                    case ObjectId.PandorasBox:
                        mo = ReadPandorasBox(s,format);
                        break;
                    case ObjectId.Grail:
                        mo = ReadGrail(s);
                        break;
                    case ObjectId.RandomDwelling:
                    case ObjectId.RandomDwellingFaction:
                    case ObjectId.RandomDwellingLevel:
                        mo = ReadDwelling(s,template.Id);
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
                h.PowerRating = s.Read<byte>();// max value is 8, as only 8 heroes can be active on a map
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
                s.Skip(4);
                d.AllowedFactions = s.Read<Factions>();
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
            g.Radius = s.Read<byte>(); // limited to 127
            s.Skip(3);
            return g;
        }

        private PandorasBoxObject ReadPandorasBox(MapDeserializer s, MapFormat format)
        {
            var p = new PandorasBoxObject();
            ReadMessageAndGuards(p, s, format);
            p.GainedExperience = s.Read<uint>();
            p.ManaDifference = s.Read<uint>();
            p.MoraleDifference = s.Read<byte>();
            p.LuckDifference = s.Read<byte>();
            p.Resources = ReadResources(s);
            p.PrimarySkills = ReadPrimarySkills(s);
            p.SecondarySkills = ReadSecondarySkills(s, s.Read<byte>());
            p.Artifacts = ReadArtifacts(s, format, s.Read<byte>());
            p.Spells = ReadSpells(s, s.Read<byte>());
            p.Creatures = ReadCreatures(s, format, s.Read<byte>());
            s.Skip(8);
            return p;
        }

        private MagicShrineObject ReadMagicShrine(MapDeserializer s)
        {
            var m = new MagicShrineObject();
            m.SpellId = s.ReadNullable(byte.MaxValue);
            s.Skip(3);
            return m;
        }

        private MapPlayerObject ReadPlayerObject(MapDeserializer s)
        {
            var m = new MapPlayerObject();
            m.Owner = s.Read<Player>();
            s.Skip(3);
            return m;
        }

        private MapTown ReadTown(MapDeserializer s, MapFormat format)
        {
            var m = new MapTown();
            if (format > MapFormat.RoE)
            {
                m.Identifier = s.Read<uint>();
            }
            m.Owner = s.Read<Player>();
            var hasName = s.Read<bool>();
            if (hasName)
            {
                m.Name = s.Read<string>();
            }
            var hasGarrison = s.Read<bool>();
            if (hasGarrison)
            {
                m.Garrison = ReadCreatures(s, format, 7);
            }
            m.GarrisonFormation = s.Read<Formation>();
            var hasCustomBuildings = s.Read<bool>();
            if (hasCustomBuildings)
            {
                m.BuiltBuildingIds = ReadBitmask(s, 6, 48);
                m.ForbiddenBuildingIds = ReadBitmask(s, 6, 48);
            }
            else
            {
                m.HasFort = s.Read<bool>();
            }
            if (format > MapFormat.RoE)
            {
                //obligatory spells
                // TODO: add 
                s.Skip(9);
            }
            // allowed spells
            // TODO: add
            s.Skip(9);

            m.Events = ReadEvents(s, format);
            if (format > MapFormat.AB)
            {
                m.Alignment = s.Read<byte>();
            }
            s.Skip(3);
            return m;
        }

        private TimedEvents[] ReadEvents(MapDeserializer s, MapFormat format, bool forCastle)
        {
            var count = s.Read<uint>();
            var events = new TimedEvents[count];
            for (var i = 0; i < events.Length; i++)
            {
                var e = new TimedEvents();
                e.Name = s.Read<string>();
                e.Message = s.Read<string>();
                e.Resources = ReadResources(s);
                e.Players = s.Read<Player>();
                if (format > MapFormat.AB)
                {
                    e.HumanAffected = s.Read<bool>();
                }
                else
                {
                    e.HumanAffected = true;
                }
                e.ComputerAffected = s.Read<bool>();
                e.FirstOccurence = s.Read<ushort>();
                e.RepeatEvery = s.Read<byte>();
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
            for (int i = 0; i < creatures.Length; i++)
            {
                creatures[i] = s.Read<ushort>();
            }
            return creatures;

        }

        private MapArtifact ReadArtifact(MapDeserializer s, MapFormat format, ObjectId id)
        {
            var a = new MapArtifact();
            ReadMessageAndGuards(a, s, format);
            if (id == ObjectId.SpellScroll)
            {
                a.SpellId = s.Read<uint>();
            }
            return a;
        }

        private Garrison ReadGarrison(MapDeserializer s, MapFormat format)
        {
            var g = new Garrison();
            g.Owner = s.Read<Player>();
            s.Skip(3);
            g.Creatues = ReadCreatures(s, format, 7);
            if (format == MapFormat.RoE)
            {
                g.UnitsAreRemovable = true;
            }
            else
            {
                g.UnitsAreRemovable = s.Read<bool>();
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

        private Scholar ReadScholar(MapDeserializer s)
        {
            var sc = new Scholar();
            sc.BonusType = s.Read<ScholarBonusType>();
            sc.BonusId = s.Read<byte>();
            s.Skip(6);
            return sc;
        }

        private WitchHut ReadWitchHut(MapDeserializer s, MapFormat format)
        {
            var h = new WitchHut();
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
                case RewardType.Experience:
                case RewardType.SpellPoints:
                    r.Value = s.Read<uint>();
                    break;
                case RewardType.Morale:
                case RewardType.Luck:
                    r.Modifier = s.Read<byte>();
                    break;
                case RewardType.Resource:
                    r.ResourceType = s.Read<Resource>();
                    var value = s.Read<uint>();
                    //only the first 3 bytes are used
                    r.Value = value & 0x00ffffff;
                    break;
                case RewardType.PrimarySkill:
                    r.SkillType = s.Read<PrimarySkillType>();
                    r.Value = s.Read<byte>();
                    break;
                case RewardType.SecondarySkill:
                    r.SecondarySkillId = s.Read<byte>();
                    r.Value = s.Read<byte>();
                    break;
                case RewardType.Artifact:
                    r.ItemId = ReadVersionDependantId(s, format).Value;
                    break;
                case RewardType.Spell:
                    r.ItemId = s.Read<byte>();
                    break;
                case RewardType.Creatures:
                    r.ItemId = ReadVersionDependantId(s, format).Value;
                    r.Value = s.Read<ushort>();
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
                    q.Artifacts = new[] {artifactId.Value};
                }
                return q;
            }

            q.Type = s.Read<QuestType>();
            switch (q.Type)
            {
                case QuestType.None:
                    break;
                case QuestType.AchievePrimarySkillLevel:
                    q.Skills = ReadPrimarySkills(s);
                    break;
                case QuestType.AchieveExperienceLevel:
                    q.Experience = s.Read<uint>();
                    break;
                case QuestType.DefeatASpecificHero:
                case QuestType.DefeatASpecificMonster:
                    // NOTE: Position or ID?
                    q.Location = ReadPosition(s);
                    s.Skip(1);
                    break;
                case QuestType.ReturnWithArtifacts:
                    var count = s.Read<byte>();
                    var artifactIds = new int[count];
                    for (var i = 0; i < artifactIds.Length; i++)
                    {
                        artifactIds[i] = s.Read<ushort>();
                    }
                    q.Artifacts = artifactIds;
                    break;
                case QuestType.ReturnWithCreatures:
                    q.Creatues = ReadCreatures(s, format, s.Read<byte>());
                    break;
                case QuestType.ReturnWithResources:
                    q.Resources = ReadResources(s);
                    break;
                case QuestType.BeASpecificHero:
                    q.HeroId = s.Read<byte>();
                    break;
                case QuestType.BelongToASpecificPlayer:
                    q.PlayerId = s.Read<Player>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unknkown quest type " + q.Type);
            }
            return q;
        }

        private MapObject ReadMessageObject(MapDeserializer s)
        {
            var m = new MapObject
            {
                Message = s.Read<string>()
            };
            s.Skip(4);
            return m;
        }

        private MapMonster ReadMapMonster(MapDeserializer s, MapFormat format)
        {
            var m = new MapMonster();
            if (format > MapFormat.RoE)
            {
                m.Identifier = s.Read<uint>();
            }
            m.Count = s.Read<ushort>();
            m.Disposition = s.Read<Disposition>();
            var hasMessage = s.Read<bool>();
            if (hasMessage)
            {
                m.Message = s.Read<string>();

                //TODO: should it be inside of that 
                m.Resources = ReadResources(s);
                m.ArtifactId = ReadVersionDependantId(s, format);
            }
            m.AlwaysAttacts = s.Read<bool>();
            m.KeepsSize = s.Read<bool>();
            s.Skip(2);
            return m;
        }

        private MapObject ReadMapHero(MapDeserializer s, MapFormat format)
        {
            var h = new MapHeroInstance();
            if (format > MapFormat.RoE)
            {
                h.Indentifier = s.Read<uint>();
            }
            h.Owner = s.Read<Player>();
            h.SubId = s.Read<byte>();
            var hasName = s.Read<bool>();
            if (hasName)
            {
                h.Name = s.Read<string>();
            }
            if (format > MapFormat.AB)
            {
                var hasExperience = s.Read<bool>();
                if (hasExperience)
                {
                    h.Experience = s.Read<uint>();
                }
            }
            else
            {
                h.Experience = s.ReadNullable<uint>(0);
            }
            var hasPotrait = s.Read<bool>();
            if (hasPotrait)
            {
                h.PortraitId = s.Read<byte>();
            }
            var hasSecondarySkills = s.Read<bool>();
            if (hasSecondarySkills)
            {
                var count = s.Read<int>();
                h.SecondarySkills = ReadSecondarySkills(s, count);
            }
            var hasArmy = s.Read<bool>();
            if (hasArmy)
            {
                h.Army = ReadCreatures(s, format, 7);
            }
            h.ArmyFormationType = s.Read<byte>();

            h.Inventory = ReadHeroInventory(s, format);
            h.PatrolRadius = s.ReadNullable(byte.MaxValue);
            if (format > MapFormat.RoE)
            {
                var hasBio = s.Read<bool>();
                if (hasBio)
                {
                    h.Bio = s.Read<string>();
                }
                h.Sex = s.ReadNullable((HeroSex) byte.MaxValue);
            }
            if (format > MapFormat.AB)
            {
                var hasSpells = s.Read<bool>();
                if (hasSpells)
                {
                    h.Spells = s.Read<BitArray>(9).OfType<bool>().ToArray();
                }
            }
            else if (format == MapFormat.AB)
            {
                var spellId = s.ReadNullable(byte.MaxValue);
                if (spellId.HasValue)
                {
                    // NOTE: not sure this is the right thing
                    h.Spells = new bool[spellId.Value + 1];
                    h.Spells[spellId.Value] = true;
                }
            }
            if (format > MapFormat.AB)
            {
                var hasCustomPrimarySkills = s.Read<bool>();
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
            var e = new MapEvent();
            ReadMessageAndGuards(e, s, format);
            e.GainedExperience = s.Read<int>();
            e.ManaDifference = s.Read<int>();
            e.MoraleDifference = s.Read<byte>();
            e.LuckDifference = s.Read<byte>();
            e.Resources = ReadResources(s);
            e.PrimarySkills = ReadPrimarySkills(s);
            e.SecondarySkills = ReadSecondarySkills(s, s.Read<byte>());
            e.Artifacts = ReadArtifacts(s, format, s.Read<byte>());
            e.Spells = ReadSpells(s, s.Read<byte>());
            e.Creatures = ReadCreatures(s, format, s.Read<byte>());
            s.Skip(8);
            e.CanBeTriggeredByPlayers = s.Read<Players>();
            e.CanBeTriggeredByAI = s.Read<bool>();
            e.CancelAfterFirstVisit = s.Read<bool>();
            s.Skip(4);
            return e;
        }

        private int[] ReadArtifacts(MapDeserializer s, MapFormat format, byte artifactCount)
        {
            var artifacts = new int[artifactCount];
            for (var i = 0; i < artifactCount; i++)
            {
                artifacts[i] = ReadVersionDependantId(s, format).Value;
            }
            return artifacts;
        }

        private static int[] ReadSpells(MapDeserializer s, byte spellCount)
        {
            var spells = new int[spellCount];
            for (var i = 0; i < spellCount; i++)
            {
                spells[i] = s.Read<byte>();
            }
            return spells;
        }

        private void ReadMessageAndGuards(MapObject o, MapDeserializer s, MapFormat format)
        {
            var hasMessage = s.Read<bool>();
            if (hasMessage)
            {
                o.Message = s.Read<string>();
                // NOTE: does it belong inside of this if?
                var hasGuards = s.Read<bool>();
                if (hasGuards)
                {
                    o.Guards = ReadCreatures(s, format, 7);
                }
            }
        }

        private MapCreature[] ReadCreatures(MapDeserializer s, MapFormat format, byte creatureCount)
        {
            var creatures = new MapCreature[creatureCount];
            for (var i = 0; i < creatureCount; i++)
            {
                var typeId = ReadVersionDependantId(s, format);
                var count = s.Read<ushort>();
                if (typeId.HasValue)
                {
                    creatures[i] = new MapCreature
                    {
                        TypeId = typeId.Value,
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
                var value = s.Read<int>();
                resources.Add(key, value);
            }
            return resources;
        }

        private CustomObject[] ReadCustomObjects(MapDeserializer s, MapFormat format)
        {
            var count = s.Read<int>();
            if (count == 0)
            {
                return null;
            }
            var co = new CustomObject[count];
            for (var i = 0; i < count; i++)
            {
                var o = new CustomObject();
                o.AnimationFile = s.Read<string>();
                var blockMask = new bool[6];
                var visitMask = new bool[6];
                for (var j = 0; j < blockMask.Length; j++)
                {
                    blockMask[j] = s.Read<bool>();
                }
                for (var j = 0; j < visitMask.Length; j++)
                {
                    visitMask[j] = s.Read<bool>();
                }
                o.SupportedTerrainTypes = s.Read<Terrains>();
                o.SupportedTerrainTypes2 = s.Read<Terrains>();
                o.Id = s.Read<ObjectId>();
                o.SubId = s.Read<int>();
                o.Type = s.Read<ObjectType>();
                o.PrintPriority = s.Read<byte>();

                s.Skip(16); //why?
                co[i] = o;
            }
            return co;
        }

        private MapTerrain ReadTerrain(MapDeserializer s, MapHeader header)
        {
            var terrain = new MapTerrain();

            terrain.Ground = ReadTerrainLevel(s, header, 0);
            if (header.HasSecondLevel)
            {
                terrain.Undrground = ReadTerrainLevel(s, header, 1);
            }

            return terrain;
        }

        private MapTile[] ReadTerrainLevel(MapDeserializer s, MapHeader header, int level)
        {
            var tiles = new List<MapTile>();
            for (var x = 0; x < header.Size; x++)
            {
                for (var y = 0; y < header.Size; y++)
                {
                    tiles.Add(new MapTile(x, y, level)
                    {
                        TerrainType = s.Read<byte>(),
                        TerrainView = s.Read<byte>(),
                        RiverType = s.Read<byte>(),
                        RiverDirection = s.Read<byte>(),
                        RoadType = s.Read<byte>(),
                        RoadDirection = s.Read<byte>(),
                        Flags = s.Read<byte>()
                    });
                }
            }
            return tiles.ToArray();
        }

        private MapHeroDefinition[] ReadPredefinedHeroes(MapDeserializer s, MapFormat format)
        {
            // is there a way to be smart and detect it instead?
            const int heroCount = 156;
            var list = new List<MapHeroDefinition>();
            if (format > MapFormat.AB)
            {
                for (var id = 0; id < heroCount; id++)
                {
                    var isCustom = s.Read<bool>();
                    if (isCustom == false)
                    {
                        continue;
                    }
                    var h = new MapHeroDefinition {HeroId = id};
                    list.Add(h);
                    var hasExperience = s.Read<bool>();
                    if (hasExperience)
                    {
                        h.Experience = s.Read<int>();
                    }
                    var hasSecondarySkills = s.Read<bool>();
                    if (hasSecondarySkills)
                    {
                        var secondarySkillCount = s.Read<int>();
                        var skills = ReadSecondarySkills(s, secondarySkillCount);
                        h.SecondarySkills = skills;
                    }
                    var hasAtrifacts = s.Read<bool>();
                    if (hasAtrifacts)
                    {
                        h.Inventory = ReadHeroInventory(s, format);
                    }
                    var hasBio = s.Read<bool>();
                    if (hasBio)
                    {
                        h.Bio = s.Read<string>();
                    }
                    h.Sex = s.ReadNullable((HeroSex) byte.MaxValue);
                    var hasCustomSpells = s.Read<bool>();
                    if (hasCustomSpells)
                    {
                        h.BitMaskSpells = s.Read<BitArray>(9).OfType<bool>().ToArray();
                    }
                    var hasPrimarySkills = s.Read<bool>();
                    if (hasPrimarySkills)
                    {
                        var primarySkills = ReadPrimarySkills(s);
                        h.PrimarySkills = primarySkills;
                    }
                }
            }
            return list.ToArray();
        }

        private static SecondarySkill[] ReadSecondarySkills(MapDeserializer s, int secondarySkillCount)
        {
            var skills = new SecondarySkill[secondarySkillCount];
            for (var i = 0; i < secondarySkillCount; i++)
            {
                skills[i] = new SecondarySkill
                {
                    Type = s.Read<byte>(),
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
                var value = s.Read<byte>();
                primarySkills.Add(type, value);
            }
            return primarySkills;
        }

        private HeroArtifact[] ReadHeroInventory(MapDeserializer s, MapFormat format)
        {
            var artifactSlots = 16;
            var artifacts = new List<HeroArtifact>();
            for (var i = 0; i < artifactSlots; i++)
            {
                ReadArtifactForSlot(s, format, artifacts, i);
            }
            if (format > MapFormat.AB)
            {
                ReadArtifactForSlot(s, format, artifacts, 16);
            }

            ReadArtifactForSlot(s, format, artifacts, 17); //spellbook
            if (format > MapFormat.RoE)
            {
                ReadArtifactForSlot(s, format, artifacts, 18);
            }
            else
            {
                s.Skip(1);
            }
            //bag artifacts
            var bagSize = s.Read<ushort>();
            for (var i = 0; i < bagSize; i++)
            {
                ReadArtifactForSlot(s, format, artifacts, 19 + i);
            }
            return artifacts.ToArray();
        }

        private void ReadArtifactForSlot(MapDeserializer s, MapFormat format, List<HeroArtifact> artifacts, int i)
        {
            var artifactId = ReadVersionDependantId(s, format);
            if (artifactId.HasValue)
            {
                artifacts.Add(new HeroArtifact
                {
                    ArtifactId = artifactId.Value,
                    Slot = i
                });
            }
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
            var count = s.Read<int>();
            var rumors = new MapRumor[count];
            for (var i = 0; i < count; i++)
            {
                var r = new MapRumor
                {
                    Name = s.Read<string>(),
                    Value = s.Read<string>()
                };
                rumors[i] = r;
            }
            return rumors;
        }

        private MapSpellsAndAbilities ReadAllowedSpellsAndAbilities(MapDeserializer s, MapFormat format)
        {
            if (format < MapFormat.SoD) return null;

            var sa = new MapSpellsAndAbilities
            {
                BitMaskSpells = s.Read<BitArray>(9).OfType<bool>().ToArray(),
                BitMaskAbilities = s.Read<BitArray>(4).OfType<bool>().ToArray()
            };
            return sa;
        }

        private MapArtifacts ReadAllowedArtifacts(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE) return null;
            var byteCount = format == MapFormat.AB ? 17 : 18;
            var bits = s.Read<BitArray>(byteCount);
            return new MapArtifacts
            {
                BitMask = bits.OfType<bool>().ToArray()
            };
        }

        private DisposedHero[] ReadDisposedHeroes(MapDeserializer s, MapFormat format)
        {
            DisposedHero[] dh = null;
            if (format >= MapFormat.SoD)
            {
                var count = s.Read<byte>();
                dh = new DisposedHero[count];
                for (var i = 0; i < dh.Length; i++)
                {
                    dh[i] = new DisposedHero
                    {
                        HeroId = s.Read<byte>(),
                        PortraitId = s.Read<byte>(),
                        Name = s.Read<string>(),
                        Players = s.Read<byte>()
                    };
                }
            }

            //omitting NULLS
            s.Skip(31);
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
                var placeholderCount = s.Read<int>();
                if (placeholderCount > 0)
                {
                    var placeholderHeroes = new int[placeholderCount];
                    for (var i = 0; i < placeholderCount; i++)
                    {
                        placeholderHeroes[i] = s.Read<byte>();
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
                        lc.Value = s.Read<short>();

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
                vc.AllowNormalVictory = s.Read<bool>();
                vc.AppliesToAI = s.Read<bool>();
                switch (type)
                {
                    case VictoryConditionType.Artifact:
                        vc.ObjectType = s.Read<byte>();
                        if (mapFormat > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        break;
                    case VictoryConditionType.GatherTroop:
                        vc.ObjectType = s.Read<byte>();
                        if (mapFormat > MapFormat.RoE)
                        {
                            s.Skip(1);
                        }
                        vc.Value = s.Read<int>();
                        break;
                    case VictoryConditionType.GatherResource:
                        vc.ObjectType = s.Read<byte>();
                        vc.Value = s.Read<int>();
                        break;
                    case VictoryConditionType.BuildCity:
                        vc.Position = ReadPosition(s);
                        vc.ObjectType = s.Read<byte>();
                        vc.Value = s.Read<int>();
                        vc.HallLevel = s.Read<BuildingLevel3>();
                        vc.CastleLevel = s.Read<BuildingLevel3>();
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
                        vc.ObjectType = s.Read<byte>();
                        vc.Position = ReadPosition(s);
                        break;
                    case VictoryConditionType.TakeDwellings:
                    case VictoryConditionType.TakeMines:
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
            player.CanHumanPlay = s.Read<bool>();
            player.CanAIPlay = s.Read<bool>();
            if (player.Disabled)
            {
                s.Skip(GetPlayerBytesToSkip(format));
            }
            else
            {
                player.AITactic = s.Read<AITactic>();
                if (format > MapFormat.AB)
                {
                    player.P7 = s.Read<byte>();
                }
                player.AllowedFactions = Fractions(s, format);
                player.IsFactionRandom = s.Read<bool>();
                player.HasHomeTown = s.Read<bool>();

                if (player.HasHomeTown)
                {
                    if (format != MapFormat.RoE)
                    {
                        player.GenerateHeroAtMainTown = s.Read<bool>();
                        player.GenerateHero = s.Read<bool>();
                    }
                    player.HomeTownPosition = ReadPosition(s);
                }
                player.HasRandomHero = s.Read<bool>();
                player.MainCustomHeroId = s.ReadNullable(byte.MaxValue);
                if (player.MainCustomHeroId.HasValue)
                {
                    player.MainCustomHeroPortraitId = s.ReadNullable(byte.MaxValue);
                    player.MainCustomHeroName = s.Read<string>();
                }
                if (format > MapFormat.RoE)
                {
                    player.PowerPlaceholders = s.Read<byte>();
                    var heroCount = s.Read<int>();
                    for (var i = 0; i < heroCount; i++)
                    {
                        player.AddHero(ReadHero(s));
                    }
                }
            }
            return player;
        }

        private HeroInfo ReadHero(MapDeserializer s)
        {
            return new HeroInfo
            {
                Id = s.Read<byte>(),
                Name = s.Read<string>()
            };
        }

        private MapPosition ReadPosition(MapDeserializer s)
        {
            return new MapPosition
            {
                X = s.Read<byte>(),
                Y = s.Read<byte>(),
                Z = s.Read<byte>()
            };
        }

        private static Factions Fractions(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE)
            {
                return (Factions) s.Read<byte>();
            }
            return (Factions) s.Read<short>();
        }

        private int GetPlayerBytesToSkip(MapFormat format)
        {
            switch (format)
            {
                case MapFormat.RoE:
                    return 6;
                case MapFormat.AB:
                    return 12;
                case MapFormat.SoD:
                case MapFormat.HotA:
                    return 13;
                default:
                    return 13;
            }
        }
    }
}