using System;
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
        private readonly IdMappings ids;

        public MapReader(IdMappings ids)
        {
            this.ids = ids;
        }

        public H3Map Read(MapDeserializer s)
        {
            // https://github.com/potmdehex/homm3tools/blob/master/h3m/h3mlib/h3m_structures/h3m_description.english.txt
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

            ids.SetCurrentVersion(IsHota(info.Format) ? MapFormat.HotA : info.Format);
            if (IsHota(info.Format))
            {
                // there's either 4 empty bytes or 1 followed by 5 empty ones.
                // not sure about the meaning of that yet...
                info.FormatSubversion = s.ReadEnum<MapFormatSubversion>();
                s.Skip(info.FormatSubversion == MapFormatSubversion.Version1 ? 5 : 3);
            }

            info.HasPlayers = s.ReadBool();
            info.Size = s.Read4ByteNumber();
            info.HasSecondLevel = s.ReadBool();
            info.Name = s.ReadString(30);
            info.Description = s.ReadString(300);
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
                    player.TeamId = s.Read1ByteNumber(maxValue: (byte) (teamCount - 1));
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
            return format == MapFormat.RoE ||
                   format == MapFormat.AB ||
                   format == MapFormat.SoD ||
                   format == MapFormat.WoG ||
                   format == MapFormat.HotA3;
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
                if (!EnumValues.IsDefined(template.EditorMenuLocation))
                {
                    Log.Information(
                        "{position}\t{location}\t{Id}\t{SubId}\t{Type}\t{AnimationFile}",
                        position,
                        template.EditorMenuLocation,
                        template.Id,
                        template.SubId,
                        template.Type,
                        template.AnimationFile
                    );
                }


                s.Skip(5); //why?
                switch (template.Id)
                {
                    case ObjectId.Event:
                        mo = ReadMapEvent(s, info.Format);
                        break;
                    case ObjectId.Hero:
                        mo = ReadMapHero(s, EnumValues.Cast<HeroType>(template.SubId), info.Format);
                        break;
                    case ObjectId.RandomHero:
                    case ObjectId.Prison:
                        mo = ReadMapHero(s, null, info.Format);
                        break;
                    case ObjectId.Monster:
                    case ObjectId.RandomMonster:
                    case ObjectId.RandomMonster1:
                    case ObjectId.RandomMonster2:
                    case ObjectId.RandomMonster3:
                    case ObjectId.RandomMonster4:
                    case ObjectId.RandomMonster5:
                    case ObjectId.RandomMonster6:
                    case ObjectId.RandomMonster7:
                        mo = ReadMapMonster(s, template.SubId, info.Format);
                        break;
                    case ObjectId.OceanBottle:
                    case ObjectId.Sign:
                        mo = ReadMessageObject(s);
                        break;
                    case ObjectId.SeersHut:
                        mo = ReadSeersHut(s, EnumValues.Cast<SeerHutType>(template.SubId), info.Format);
                        break;
                    case ObjectId.WitchHut:
                        mo = ReadWitchHut(s, info.Format, template.SubId);
                        break;
                    case ObjectId.Scholar:
                        mo = ReadScholar(s);
                        break;
                    case ObjectId.Garrison:
                        mo = ReadGarrison(s, info.Format, template.SubId, GarrisonOrientation.EastWest);
                        break;
                    case ObjectId.Garrison2:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = ReadGarrison(s, info.Format, template.SubId, GarrisonOrientation.NorthSouth);
                        break;
                    case ObjectId.Artifact:
                        mo = ReadArtifact(s, info.Format, template.SubId);
                        break;
                    case ObjectId.RandomArtifact:
                        mo = ReadArtifact(s, info.Format, template.SubId);
                        break;
                    case ObjectId.RandomTreasureArtifact:
                    case ObjectId.RandomMinorArtifact:
                    case ObjectId.RandomMajorArtifact:
                    case ObjectId.RandomRelicArtifact:
                        mo = ReadArtifact(s, info.Format, null);
                        break;
                    case ObjectId.SpellScroll:
                        mo = ReadSpellScroll(s, info.Format);
                        break;
                    case ObjectId.RandomResource:
                    case ObjectId.Resource:
                        mo = ReadMapResource(s, EnumValues.Cast<Resource>(template.SubId), info.Format);
                        break;
                    case ObjectId.RandomTown:
                    case ObjectId.Town:
                        mo = ReadTown(s, EnumValues.Cast<Faction>(template.SubId), info);
                        break;
                    case ObjectId.CreatureGenerator1:
                    case ObjectId.CreatureGenerator2:
                    case ObjectId.CreatureGenerator3:
                    case ObjectId.CreatureGenerator4:
                        mo = ReadCreatureGenerator(s, EnumValues.Cast<CreatureGeneratorType>(template.SubId));
                        break;
                    case ObjectId.Shipyard:
                    case ObjectId.Lighthouse:
                        mo = ReadPlayerObject(s);
                        break;
                    case ObjectId.Mine:
                    case ObjectId.Mine2:
                        mo = ReadMine(s, EnumValues.Cast<MineType>(template.SubId));
                        break;
                    case ObjectId.ShrineOfMagicGesture:
                    case ObjectId.ShrineOfMagicIncantation:
                    case ObjectId.ShrineOfMagicThought:
                        mo = ReadMagicShrine(s, template.Id, template.SubId);
                        break;
                    case ObjectId.PandorasBox:
                        mo = ReadPandorasBox(s, info.Format);
                        break;
                    case ObjectId.Grail:
                        mo = ReadGrail(s);
                        break;
                    case ObjectId.RandomDwelling:
                    case ObjectId.RandomDwellingLevel:
                    case ObjectId.RandomDwellingFaction:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = ReadDwelling(s, template.Id, template.SubId);
                        break;
                    case ObjectId.QuestGuard:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = ReadQuest(s, info.Format);
                        break;
                    case ObjectId.HeroPlaceholder:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = ReadHeroPlaceholder(s);
                        break;
                    case ObjectId.CreatureBank:
                        mo = new MapObject<CreatureBankType>(template.SubId);
                        break;
                    case ObjectId.BorderGate:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = new MapObject<ObjectColor>(template.SubId);
                        break;
                    case ObjectId.BorderGuard:
                        mo = new MapObject<ObjectColor>(template.SubId);
                        break;
                    case ObjectId.Object:
                        mo = new WoGObject(template.SubId);
                        break;
                    case ObjectId.Cartographer:
                        mo = new MapObject<CartographerType>(template.SubId);
                        break;
                    case ObjectId.TreasureChest:
                        mo = new MapObject<TreasureChestType>(template.SubId);
                        break;
                    case ObjectId.ResourceWarehouse:
                        mo = new MapObject<Resource>(template.SubId);
                        break;
                    case ObjectId.HillFort:
                        mo = new MapObject<HillFortType>(template.SubId);
                        break;
                    case ObjectId.SchoolOfMagic:
                        mo = new MapObject<SchoolOfMagicType>(template.SubId);
                        break;
                    case ObjectId.Tavern:
                    case ObjectId.LearningStone:
                    case ObjectId.SubterraneanGate:
                    case ObjectId.LibraryOfEnlightenment:
                    case ObjectId.IdolOfFortune:
                        mo = new MapObject<ObjectVariantType>(template.SubId);
                        break;
                    case ObjectId.RedwoodObservatory:
                        mo = new MapObject<ObservatoryType>(template.SubId);
                        break;
                    case ObjectId.MonolithTwoWay:
                        mo = new MapObject<MonolithTwoWayType>(template.SubId);
                        break;
                    case ObjectId.WarMachineFactory:
                        mo = new MapObject<WarMachineFactoryType>(template.SubId);
                        break;
                    case ObjectId.MonolithOneWayEntrance:
                    case ObjectId.MonolithOneWayExit:
                        mo = new MapObject<MonolithOneWayType>(template.SubId);
                        break;
                    case ObjectId.MagicalTerrain:
                        mo = new MapObject<MagicalTerrainType>(template.SubId);
                        break;
                    case ObjectId.Building:
                        mo = new MapObject<BuildingType>(template.SubId);
                        break;
                    case ObjectId.SeaObject:
                        mo = new MapObject<SeaObjectType>(template.SubId);
                        break;
                    case ObjectId.Building2:
                        mo = new MapObject<Building2Type>(template.SubId);
                        break;
                    case ObjectId.FreelancersGuild:
                        RequireVersionAtLeast(info, MapFormat.AB);
                        mo = new MapObject();
                        break;
                    default:
                        mo = new MapObject();
                        break;
                }

                mo.Position = position;
                mo.Template = template;
                LogUnexpectedTemplateSubId(mo);
                objects[i] = mo;
            }

            return objects;
        }

        private CreatureGeneratorObject ReadCreatureGenerator(MapDeserializer s, CreatureGeneratorType type)
        {
            var g = new CreatureGeneratorObject
            {
                Type = type,
                Owner = s.ReadEnum<Player>()
            };
            s.Skip(3);
            return g;
        }

        private void LogUnexpectedTemplateSubId(MapObject mo)
        {
            switch (mo.Template.Id)
            {
                case ObjectId.Mine:
                case ObjectId.Mine2:
                case ObjectId.Town:
                case ObjectId.Hero:
                case ObjectId.Monster:
                case ObjectId.Resource:
                case ObjectId.SeersHut:
                case ObjectId.CreatureBank:
                case ObjectId.CreatureGenerator1:
                case ObjectId.CreatureGenerator2:
                case ObjectId.CreatureGenerator3:
                case ObjectId.CreatureGenerator4:
                case ObjectId.RandomMonster1:
                case ObjectId.RandomMonster2:
                case ObjectId.RandomMonster3:
                case ObjectId.RandomMonster4:
                case ObjectId.RandomMonster5:
                case ObjectId.RandomMonster6:
                case ObjectId.RandomMonster7:
                case ObjectId.BorderGate:
                case ObjectId.BorderGuard:
                case ObjectId.Object:
                case ObjectId.Artifact:
                case ObjectId.Cartographer:
                case ObjectId.TreasureChest:
                case ObjectId.LearningStone:
                case ObjectId.SubterraneanGate:
                case ObjectId.LibraryOfEnlightenment:
                case ObjectId.WitchHut:
                case ObjectId.Tavern:
                case ObjectId.RandomDwellingLevel:
                case ObjectId.RandomDwellingFaction:
                case ObjectId.Garrison:
                case ObjectId.Garrison2:
                case ObjectId.IdolOfFortune:
                case ObjectId.ResourceWarehouse:
                case ObjectId.HillFort:
                case ObjectId.SchoolOfMagic:
                case ObjectId.ShrineOfMagicIncantation:
                case ObjectId.RedwoodObservatory:
                case ObjectId.MonolithTwoWay:
                case ObjectId.MonolithOneWayEntrance:
                case ObjectId.MonolithOneWayExit:
                case ObjectId.WarMachineFactory:
                case ObjectId.SeaObject:
                case ObjectId.Building:
                case ObjectId.Building2:
                case ObjectId.MagicalTerrain:
                case ObjectId.ShrineOfMagicGesture:
                    return;
                case ObjectId.KeymastersTent:
                    if (mo.Template.SubId > 8)
                    {
                        LogUnexpectedType(mo);
                    }

                    return;
                // HotA decorative objects. SubId defines the type but they are purely ornamental
                // TODO: confirm they are roughly the same as ObjectId.DecorativeObject or if it should be renamed
                case ObjectId.DecorativeObject:
                case ObjectId.DecorativeObject2:
                case ObjectId.DecorativeObject3:
                    return;
                case ObjectId.ShrineOfMagicThought:
                case ObjectId.MagicWell:
                    if (mo.Template.SubId > 1)
                    {
                        LogUnexpectedType(mo);
                    }

                    return;
                case ObjectId.Boat:
                    if (mo.Template.SubId > 5)
                    {
                        LogUnexpectedType(mo);
                    }

                    return;
                default:
                    if (mo.Template.SubId == 0) return;
                    LogUnexpectedType(mo);
                    return;
            }
        }

        private static void LogUnexpectedType(MapObject mo)
        {
            Log.Information("Unexpected Object Subtype {subid} for object {id}:{type} {animationFile} {location}",
                mo.Template.SubId,
                mo.Template.Id,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }

        private MapObject ReadMine(MapDeserializer s, MineType mineType)
        {
            if (mineType == MineType.AbandonedMine)
            {
                return ReadAbandonedMine(s);
            }

            var m = new MineObject {MineType = mineType};
            m.Owner = s.ReadEnum<Player>();
            s.Skip(3);
            return m;
        }

        private MapObject ReadAbandonedMine(MapDeserializer s)
        {
            var m = new AbandonedMineObject();
            m.PotentialResources = s.ReadEnum<Resources>();
            return m;
        }

        private ResourceObject ReadMapResource(MapDeserializer s, Resource resource, MapFormat format)
        {
            var r = new ResourceObject();
            r.Resource = resource;
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

        private DwellingObject ReadDwelling(MapDeserializer s, ObjectId id, int subId)
        {
            var d = new DwellingObject();
            d.Player = s.ReadEnum<Player>();
            s.Skip(3);
            if (id == ObjectId.RandomDwellingFaction)
            {
                d.RandomDwellingFaction = EnumValues.Cast<Faction>(subId);
            }
            else
            {
                var sameAsCastleId = s.Read4ByteNumberLong();
                if (sameAsCastleId != 0)
                {
                    d.FactionSameAsCastleId = sameAsCastleId;
                }
                else
                {
                    d.AllowedFactions = s.ReadEnum<Factions>();
                }
            }

            if (id == ObjectId.RandomDwellingLevel)
            {
                d.MinLevel = d.MaxLevel = EnumValues.Cast<UnitLevel>(subId);
            }
            else
            {
                d.MinLevel = s.ReadEnum<UnitLevel>();
                d.MaxLevel = s.ReadEnum<UnitLevel>();
            }

            return d;
        }

        private GrailObject ReadGrail(MapDeserializer s)
        {
            var g = new GrailObject();
            g.Radius = s.Read4ByteNumber(0, 127);
            return g;
        }

        private PandorasBoxObject ReadPandorasBox(MapDeserializer s, MapFormat format)
        {
            var p = new PandorasBoxObject();
            ReadMessageAndGuards(p, s, format);
            p.GainedExperience = s.Read4ByteNumber(0, 99999999);
            p.ManaDifference = s.Read4ByteNumber(-9999, 9999);
            p.MoraleDifference = s.ReadEnum<LuckMoraleModifier>();
            p.LuckDifference = s.ReadEnum<LuckMoraleModifier>();
            p.Resources = ReadResources(s, format, allowNegative: true);
            p.PrimarySkills = ReadPrimarySkills(s);
            p.SecondarySkills = ReadSecondarySkills(s, s.Read1ByteNumber(maxValue: 8));
            p.Artifacts = ReadArtifacts(s, format, s.Read1ByteNumber());
            p.Spells = ReadSpells(s, s.Read1ByteNumber());
            p.Monsters = ReadCreatures(s, format, s.Read1ByteNumber(maxValue: 7));
            s.Skip(8);
            return p;
        }

        private MagicShrineObject ReadMagicShrine(MapDeserializer s, ObjectId templateId, int templateSubId)
        {
            var m = new MagicShrineObject();
            m.SpellLevel = MapSpellLevel(templateId, templateSubId);
            var spellId = s.Read1ByteNumber();
            if (spellId != byte.MaxValue)
            {
                m.Spell = ids.GetSpell(spellId);
            }

            if (templateId == ObjectId.ShrineOfMagicGesture)
            {
                m.Type = EnumValues.Cast<ShrineType>(templateSubId);
            }

            s.Skip(3);
            return m;
        }

        private MagicShrineSpellLevel MapSpellLevel(ObjectId templateId, int templateSubId)
        {
            if (templateId == ObjectId.ShrineOfMagicIncantation)
            {
                return EnumValues.Cast<MagicShrineSpellLevel>(templateSubId);
            }

            if (templateId == ObjectId.ShrineOfMagicGesture)
            {
                return MagicShrineSpellLevel.Two;
            }

            Debug.Assert(templateId == ObjectId.ShrineOfMagicThought);
            return MagicShrineSpellLevel.Three;
        }

        private PlayerOwnedObject ReadPlayerObject(MapDeserializer s)
        {
            var m = new PlayerOwnedObject();
            m.Owner = s.ReadEnum<Player>();
            s.Skip(3);
            return m;
        }

        private TownObject ReadTown(MapDeserializer s, Faction faction, MapInfo info)
        {
            var m = new TownObject();
            m.Faction = faction;
            if (info.Format > MapFormat.RoE)
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
                m.Garrison = ReadCreatures(s, info.Format, 7);
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

            if (info.Format > MapFormat.RoE)
            {
                m.SpellsThatMustAppear = ReadSpellsFromBitmask(s, activeBitValue: true);
            }

            m.SpellsThatMayAppear = ReadSpellsFromBitmask(s);
            if (info.FormatSubversion == MapFormatSubversion.Version1)
            {
                m.AllowSpellResearch = s.ReadBool();
            }

            m.Events = ReadEvents(s, info.Format, true);
            if (info.Format > MapFormat.AB)
            {
                // this only applies to random castles
                m.Alignment = s.ReadEnum<RandomTownAlignment>();
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
                e.Resources = ReadResources(s, format, allowNegative: true);
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

        private SpellScrollObject ReadSpellScroll(MapDeserializer s, MapFormat format)
        {
            var spellScroll = new SpellScrollObject();
            ReadMessageAndGuards(spellScroll, s, format);
            spellScroll.Spell = ids.GetSpell(s.Read4ByteNumber());
            return spellScroll;
        }

        private MapObject ReadArtifact(MapDeserializer s, MapFormat format, int? artifactId)
        {
            var a = new ArtifactObject();
            ReadMessageAndGuards(a, s, format);
            if (artifactId.HasValue)
            {
                a.Artifact = ids.GetArtifact(artifactId.Value);
            }

            return a;
        }

        private GarrisonObject ReadGarrison(MapDeserializer s, MapFormat format, int garrisonType,
            GarrisonOrientation orientation)
        {
            var g = new GarrisonObject(garrisonType)
            {
                Owner = s.ReadEnum<Player>(),
                Orientation = orientation
            };
            s.Skip(3);
            g.Creatues = ReadCreatures(s, format, 7);
            g.UnitsAreRemovable = format == MapFormat.RoE || s.ReadBool();
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

        private WitchHutObject ReadWitchHut(MapDeserializer s, MapFormat format, int rawVariant)
        {
            var h = new WitchHutObject(rawVariant);
            if (format > MapFormat.RoE)
            {
                var skills = new List<SecondarySkillType>();
                var flags = s.ReadBitmask(4);
                for (var i = 0; i < flags.Length; i++)
                {
                    if (flags[i])
                    {
                        skills.Add(EnumValues.Cast<SecondarySkillType>(i));
                    }
                }

                h.AllowedSkills = skills.ToArray();
            }

            return h;
        }

        private SeerHutObject ReadSeersHut(MapDeserializer s, SeerHutType type, MapFormat format)
        {
            var h = new SeerHutObject();
            h.Type = type;
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
                    r.Value = s.Read4ByteNumber(1, 99999999);
                    break;
                case RewardType.SpellPoints:
                    r.Value = s.Read4ByteNumber(1, 999);
                    break;
                case RewardType.Morale:
                case RewardType.Luck:
                    r.LuckMorale = s.ReadEnum<LuckMoraleModifier>();
                    break;
                case RewardType.Resource:
                    r.Resource = s.ReadEnum<Resource>();
                    r.Value = s.Read4ByteNumber(1, 99999);
                    break;
                case RewardType.PrimarySkill:
                    r.SkillType = s.ReadEnum<PrimarySkillType>();
                    r.Value = s.Read1ByteSignedNumber(1, 99);
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
                    r.Value = s.Read2ByteNumber(1, 9999);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown reward type: {r.Type}");
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
                    // NOTE: not sure why HotA allows up to 9 creatures but well... they do
                    q.Creatues = ReadCreatures(s, format, s.Read1ByteNumber(maxValue: (byte) (IsHota(format) ? 9 : 7)));
                    break;
                case QuestType.ReturnWithResources:
                    q.Resources = ReadResources(s, format);
                    break;
                case QuestType.BeASpecificHero:
                    q.HeroId = s.Read1ByteNumber();
                    break;
                case QuestType.BelongToASpecificPlayer:
                    q.PlayerId = s.ReadEnum<Player>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknkown quest type {q.Type}");
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

        private MonsterObject ReadMapMonster(MapDeserializer s, int monsterId, MapFormat format)
        {
            var m = new MonsterObject();
            m.Type = ids.GetMonster(monsterId);
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
                m.Resources = ReadResources(s, format);
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

        private MapObject ReadMapHero(MapDeserializer s, HeroType? type, MapFormat format)
        {
            var h = new HeroObject();
            if (format > MapFormat.RoE)
            {
                h.Indentifier = s.Read4ByteNumberLong();
            }

            h.Type = type;
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
                if (spellId != byte.MaxValue && // no spell
                    spellId != (byte.MaxValue - 1)) // has 'default'? spell
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
            e.Resources = ReadResources(s, format, allowNegative: true);
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
// we only care about range if we actually have guards
                var count = s.Read2ByteNumberSigned(0, typeId != null ? (short) 9999 : short.MaxValue);
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

        private IDictionary<Resource, int> ReadResources(MapDeserializer s, MapFormat format,
            bool allowNegative = false)
        {
            var resources = new Dictionary<Resource, int>();
            var keys = EnumValues.For<Resource>().TakeWhile(r => r <= Resource.Gold);
            foreach (var key in keys)
            {
                var value = s.Read4ByteNumber(allowNegative ? -99999 : 0, 99999);
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
                Log.Warning("Map object template count {count} looks wrong. Probably there is a bug here.", count);
            }

            var co = new MapObjectTemplate[count];
            for (var i = 0; i < count; i++)
            {
// Good description of how bit masks (block mask and visit mask) work:
// https://github.com/potmdehex/homm3tools/blob/master/h3m/h3mlib/h3m_constants/h3m.txt#L144-L171
                var o = new MapObjectTemplate();
                o.AnimationFile = s.ReadString(255 /* not really possible to verify buy they are all really short */);
                var blockMask = s.ReadBitmask(6);
                var visitMask = s.ReadBitmask(6);
                o.BlockPosition = MapPositionBitmask(blockMask);
                o.VisitPosition = MapPositionBitmask(visitMask);
                o.SupportedTerrainTypes = s.ReadEnum<Terrains>();
                o.EditorMenuLocation = s.ReadEnum<TerrainMenus>();
                o.Id = s.ReadEnum<ObjectId>();
                o.SubId = s.Read4ByteNumber();
                o.Type = s.ReadEnum<ObjectType>();
                o.IsBackground = s.ReadBool();
                s.Skip(16); //why?
                co[i] = o;
            }

            return co;
        }

        private Position MapPositionBitmask(bool[] mask)
        {
            var positions = new bool[8, 6];
            for (var i = 0; i < mask.Length; i++)
            {
                positions[i / 6, i % 6] = !mask[i];
            }

            return new Position {Positions = positions};
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
                        TerrainVariant = s.Read1ByteNumber(),
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

        /// <param name="activeBitValue">This is a terrible name. If <c>true</c> inverts the meaning of the bitmask</param>
        private Identifier[] ReadSpellsFromBitmask(MapDeserializer s, bool activeBitValue = false)
        {
            var bitmask = s.ReadBitmaskBits(70);
            var spells = new List<Identifier>();
            for (var i = 0; i < bitmask.Length; i++)
            {
                if (bitmask[i] == activeBitValue)
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
            var primarySkillTypes = EnumValues.For<PrimarySkillType>();
            var primarySkills = new Dictionary<PrimarySkillType, int>();
            foreach (var type in primarySkillTypes)
            {
                var value = s.Read1ByteSignedNumber(0, 99);
                primarySkills.Add(type, value);
            }

            return primarySkills;
        }

        private HeroArtifact[] ReadHeroInventory(MapDeserializer s, MapFormat format)
        {
            var artifacts = new List<HeroArtifact>();
            foreach (var slot in EnumValues.For<ArtifactSlot>().TakeWhile(x => x < ArtifactSlot.Misc5))
            {
                artifacts.TryAdd(ReadArtifactForSlot(s, format, slot));
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
            var count = s.Read4ByteNumber(0, 30);
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
                    skills.Add(EnumValues.Cast<SecondarySkillType>(i));
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
                var number = s.Read1ByteNumber();
                if (number == 16)
                {
                    // no idea what that is. 6 bytes started by 0x10, followed by five 0x00
                    s.Skip(5);
                    number = s.Read1ByteNumber();
                }

                s.Skip(3);

                return s.ReadBitmaskBits(number);
            }

            return s.ReadBitmaskBits(format == MapFormat.AB ? 129 : 144);
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
            var bitCount = GetAllowedHeroesCount(s, format);
            var bitmask = s.ReadBitmaskBits(bitCount);
            for (var i = 0; i < bitmask.Length; i++)
            {
                if (bitmask[i])
                {
                    heroes.AddHero(ids.GetHero(i));
                }
            }

            if (format > MapFormat.RoE)
            {
                var placeholderCount = s.Read4ByteNumber();
                if (placeholderCount > 0)
                {
                    for (var i = 0; i < placeholderCount; i++)
                    {
                        var hero = ids.GetHero(s.Read1ByteNumber());
                        heroes.AddHero(hero);
                    }
                }
            }
        }

        private static int GetAllowedHeroesCount(MapDeserializer mapDeserializer, MapFormat format)
        {
            if (IsHota(format))
            {
                return mapDeserializer.Read4ByteNumber(minValue: 0);
            }

            var byteCount = format == MapFormat.RoE ? 16 : 20;
            return byteCount * 8;
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
                        lc.Value = s.Read2ByteNumber(2, 7 * 4 * 12);
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
// if both false, player disabled, rest of the data may be garbage
            player.AITactic = s.ReadEnum<AITactic>();
            if (format > MapFormat.AB)
            {
                // 1 Configured whether what cities owns a player
                // that makes no sense
                // VCMI call it P7 (Unknown and unused): https://github.com/vcmi/vcmi/blob/develop/lib/mapping/MapFormatH3M.cpp#L206
                player.Unknown1 = s.Read1ByteNumber();
            }

            player.AllowedFactions = Fractions(s, format);
            if (player.Disabled)
            {
// if the player is disabled this can contain garbage
                // TODO: is there any meaning to that?
                s.Read1ByteNumber();
            }
            else
            {
                player.IsFactionRandom = s.ReadBool();
            }

            player.HasHomeTown = s.ReadBool();
            if (player.HasHomeTown)
            {
                if (format != MapFormat.RoE)
                {
                    player.GenerateHeroAtMainTown = s.ReadBool();
                    // 1 Chief Town player: 1-DA 0-NET
                    // VCMI call it generateHero (unused): https://github.com/vcmi/vcmi/blob/develop/lib/mapping/MapFormatH3M.cpp#L238
                    player.Unknown2 = s.Read1ByteNumber();
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
            var maxValue = (byte) Math.Min(byte.MaxValue, mapSize + 8);
            return new MapPosition
            {
                X = s.Read1ByteNumber(maxValue: maxValue, allowEmpty: allowEmpty),
                Y = s.Read1ByteNumber(maxValue: maxValue, allowEmpty: allowEmpty),
                Z = s.Read1ByteNumber(maxValue: 1, allowEmpty: allowEmpty)
            };
        }

        private static Factions Fractions(MapDeserializer s, MapFormat format)
        {
            if (format == MapFormat.RoE)
            {
                return s.ReadEnum<Factions>(1);
            }

            return s.ReadEnum<Factions>(2);
        }

        private static void RequireVersionAtLeast(MapInfo info, MapFormat version)
        {
            if (info.Format >= version) return;
            Log.Warning("This map's format is {format} but it has features requiring at least {requiredFormat}",
                info.Format,
                version);
        }
    }
}