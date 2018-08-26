using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using H3Mapper.Flags;
using H3Mapper.MapModel;
using H3Mapper.MapObjects;
using Serilog;

namespace H3Mapper
{
    public class DuplicateFinder : IDisposable
    {
        private readonly SHA1Managed sha1Managed = new SHA1Managed();
        private readonly Internal.Lookup<string, string> hashes = new Internal.Lookup<string, string>();
        private int totalCount = 0;


        public void Process(H3Map map, string mapFilePath)
        {
            var hash = BitConverter.ToString(sha1Managed.ComputeHash(BuildHash(map)));
            hashes.Add(hash, mapFilePath);
            totalCount++;
        }

        private byte[] BuildHash(H3Map map)
        {
            using (var m = new MemoryStream())
            {
                using (var writer = new BinaryWriter(m))
                {
                    WriteHeader(writer, map.Info);
                    WriteHeroes(writer, map.Heroes);
                    if (map.AllowedArtifacts != null)
                    {
                        WriteArtifacts(writer, map.AllowedArtifacts);
                    }

                    if (map.AllowedSpells != null)
                    {
                        WriteSpells(writer, map.AllowedSpells);
                    }

                    if (map.AllowedSecondarySkills != null)
                    {
                        WriteSkills(writer, map.AllowedSecondarySkills);
                    }

                    WriteRumors(writer, map.Rumors);
                    WriteTerrain(writer, map.Terrain);
                    WriteObjects(writer, map.Objects);
                    WriteEvents(writer, map.Events);
                }

                return m.ToArray();
            }
        }

        private void WriteEvents(BinaryWriter w, TimedEvents[] events)
        {
            if (events == null) return;
            foreach (var e in events.OrderBy(e => e.FirstOccurence)
                .ThenBy(e => e.RepeatEvery)
                .ThenBy(e => e.Name))
            {
                w.Write(e.Name);
                w.Write(e.Message);
                WriteResources(w, e.Resources);
                w.Write((byte) e.Players);
                w.Write(e.HumanAffected);
                w.Write(e.ComputerAffected);
                w.Write(e.FirstOccurence);
                w.Write(e.RepeatEvery);
                if (e.NewBuildings != null)
                {
                    foreach (var b in e.NewBuildings)
                    {
                        w.Write(b);
                    }
                }

                if (e.Creatures != null)
                {
                    foreach (var i in e.Creatures)
                    {
                        w.Write(i);
                    }
                }
            }
        }

        private void WriteResources(BinaryWriter writer, IDictionary<Resource, int> resources)
        {
            foreach (var pair in resources.OrderBy(x => x.Key))
            {
                writer.Write((byte) pair.Key);
                writer.Write(pair.Value);
            }
        }

        private void WriteObjects(BinaryWriter writer, MapObject[] objects)
        {
            var templates = objects.Select(o => o.Template).Distinct()
                .OrderBy(x => x.Id)
                .ThenBy(x => x.SubId)
                .ThenBy(x => x.Type)
                .ThenBy(x => x.AnimationFile)
                .ThenBy(x => x.BlockPosition.ToString()) // TODO: properly compare
                .ThenBy(x => x.VisitPosition.ToString()) // TODO: properly compare
                .ThenBy(x => x.EditorMenuLocation)
                .ThenBy(x => x.SupportedTerrainTypes)
                .ThenBy(x => x.IsBackground).ToArray();
            WriteTemplates(writer, templates);
            foreach (var o in objects.OrderBy(x => x.Position.X)
                .ThenBy(x => x.Position.Y)
                .ThenBy(x => x.Position.X)
                .ThenBy(x => x.Template.Id)
                .ThenBy(x => x.Template.SubId))
            {
                WritePosition(writer, o.Position);
                writer.Write(Array.IndexOf(templates, o.Template));
                WriteObject(writer, o);
            }
        }

        private void WriteObject(BinaryWriter w, MapObject obj)
        {
            switch (obj.Template.Id)
            {
                case ObjectId.Event:
                    WriteEvent(w, (EventObject) obj);
                    return;
                case ObjectId.Hero:
                case ObjectId.RandomHero:
                case ObjectId.Prison:
                    WriteHero(w, (HeroObject) obj);
                    return;
                case ObjectId.Monster:
                case ObjectId.RandomMonster:
                case ObjectId.RandomMonster1:
                case ObjectId.RandomMonster2:
                case ObjectId.RandomMonster3:
                case ObjectId.RandomMonster4:
                case ObjectId.RandomMonster5:
                case ObjectId.RandomMonster6:
                case ObjectId.RandomMonster7:
                    WriteMonster(w, (MonsterObject) obj);
                    return;
                case ObjectId.OceanBottle:
                case ObjectId.Sign:
                    w.Write(((MessageObject) obj).Message);
                    return;
                case ObjectId.SeersHut:
                    WriteSeerHot(w, (SeerHutObject) obj);
                    return;
                case ObjectId.WitchHut:
                    WriteWitchHut(w, (WitchHutObject) obj);
                    return;
                case ObjectId.Scholar:
                    WriteScholar(w, (ScholarObject) obj);
                    return;
                case ObjectId.Garrison:
                case ObjectId.Garrison2:
                    WriteGarrison(w, (GarrisonObject) obj);
                    return;
                case ObjectId.Artifact:
                case ObjectId.RandomArtifact:
                case ObjectId.RandomTreasureArtifact:
                case ObjectId.RandomMinorArtifact:
                case ObjectId.RandomMajorArtifact:
                case ObjectId.RandomRelicArtifact:
                    WriteArtifact(w, (ArtifactObject) obj);
                    return;
                case ObjectId.SpellScroll:
                    WriteSpellScroll(w, (SpellScrollObject) obj);
                    return;
                case ObjectId.RandomResource:
                case ObjectId.Resource:
                    WriteResource(w, (ResourceObject) obj);
                    return;
                case ObjectId.RandomTown:
                case ObjectId.Town:
                    WriteTown(w, (TownObject) obj);
                    return;
                case ObjectId.CreatureGenerator2:
                case ObjectId.CreatureGenerator3:
                    throw new NotSupportedException();
                case ObjectId.CreatureGenerator1:
                case ObjectId.CreatureGenerator4:
                case ObjectId.Shipyard:
                case ObjectId.Lighthouse:
                    WritePlayerOwnedObject(w, (PlayerOwnedObject) obj);
                    return;
                case ObjectId.Mine:
                case ObjectId.Mine2:
                    WriteMine(w, obj);
                    return;
                case ObjectId.ShrineOfMagicGesture:
                case ObjectId.ShrineOfMagicIncantation:
                case ObjectId.ShrineOfMagicThought:
                    WriteMagicShrine(w, (MagicShrineObject) obj);
                    return;
                case ObjectId.PandorasBox:
                    WritePandorasBox(w, (PandorasBoxObject) obj);
                    return;
                case ObjectId.Grail:
                    WriteGrail(w, (GrailObject) obj);
                    return;
                case ObjectId.RandomDwelling:
                case ObjectId.RandomDwellingLevel:
                case ObjectId.RandomDwellingFaction:
                    WriteDwelling(w, (DwellingObject) obj);
                    return;
                case ObjectId.QuestGuard:
                    WriteQuest(w, (QuestObject) obj);
                    return;
                case ObjectId.HeroPlaceholder:
                    WriteHeroPlaceholder(w, (HeroPlaceholderObject) obj);
                    return;
                default:
                    return;
            }
        }

        private void WriteHeroPlaceholder(BinaryWriter w, HeroPlaceholderObject o)
        {
            if (o.Id != null)
            {
                w.Write(o.Id.Value);
            }

            if (o.PowerRating.HasValue)
            {
                w.Write(o.PowerRating.Value);
            }
        }

        private void WriteDwelling(BinaryWriter w, DwellingObject o)
        {
            if (o.RandomDwellingFaction.HasValue)
            {
                w.Write((byte) o.RandomDwellingFaction.Value);
            }

            if (o.FactionSameAsCastleId.HasValue)
            {
                // TODO: What is this Id?
                w.Write(o.FactionSameAsCastleId.Value);
            }

            if (o.AllowedFactions.HasValue)
            {
                w.Write((ushort) o.AllowedFactions.Value);
            }

            w.Write((byte) o.MinLevel);
            w.Write((byte) o.MaxLevel);
        }

        private void WriteGrail(BinaryWriter w, GrailObject o)
        {
            w.Write(o.Radius);
        }

        private void WritePandorasBox(BinaryWriter w, PandorasBoxObject o)
        {
            WriteMessageAndGuards(w, o);
            w.Write(o.GainedExperience);
            w.Write(o.ManaDifference);
            w.Write((sbyte) o.MoraleDifference);
            w.Write((sbyte) o.LuckDifference);
            WriteResources(w, o.Resources);
            WriteSkills(w, o.PrimarySkills);
            WriteSkills(w, o.SecondarySkills);
            WriteArtifacts(w, o.Artifacts);
            WriteSpells(w, o.Spells);
            // TODO: Unify the usage of monsters/creatures
            WriteCreatures(w, o.Monsters);
        }


        private void WriteMagicShrine(BinaryWriter w, MagicShrineObject o)
        {
            if (o.Spell != null)
            {
                w.Write(o.Spell.Value);
            }
        }

        private void WriteMine(BinaryWriter w, MapObject o)
        {
            if (o is AbandonedMineObject am)
            {
                w.Write((int) am.PotentialResources);
                return;
            }

            WritePlayerOwnedObject(w, (MineObject) o);
        }

        private void WritePlayerOwnedObject(BinaryWriter w, PlayerOwnedObject o)
        {
            w.Write((byte) o.Owner);
        }

        private void WriteTown(BinaryWriter w, TownObject o)
        {
            w.Write(o.Identifier);
            w.Write((byte) o.Owner);
            if (o.Name != null)
            {
                w.Write(o.Name);
            }

            if (o.Garrison != null)
            {
                WriteCreatures(w, o.Garrison);
            }

            w.Write((byte) o.GarrisonFormation);

            if (o.BuiltBuildingIds != null)
            {
                foreach (var id in o.BuiltBuildingIds)
                {
                    w.Write(id);
                }
            }

            if (o.ForbiddenBuildingIds != null)
            {
                foreach (var id in o.ForbiddenBuildingIds)
                {
                    w.Write(id);
                }
            }

            w.Write(o.HasFort);
            if (o.SpellsThatMayAppear != null)
            {
                WriteSpells(w, o.SpellsThatMayAppear);
            }

            if (o.SpellsThatMustAppear != null)
            {
                WriteSpells(w, o.SpellsThatMustAppear);
            }

            w.Write(o.AllowSpellResearch);
            WriteEvents(w, o.Events);
            w.Write((byte) o.Alignment);
        }

        private void WriteResource(BinaryWriter w, ResourceObject o)
        {
            WriteMessageAndGuards(w, o);
            w.Write(o.Amount);
        }

        private void WriteSpellScroll(BinaryWriter w, SpellScrollObject o)
        {
            WriteMessageAndGuards(w, o);
            w.Write(o.Spell.Value);
        }

        private void WriteArtifact(BinaryWriter w, ArtifactObject o)
        {
            WriteMessageAndGuards(w, o);
            if (o.Artifact != null)
            {
                w.Write(o.Artifact.Value);
            }
        }

        private void WriteGarrison(BinaryWriter w, GarrisonObject o)
        {
            w.Write((byte) o.Owner);
            WriteCreatures(w, o.Creatues);
            w.Write(o.UnitsAreRemovable);
        }

        private void WriteScholar(BinaryWriter w, ScholarObject o)
        {
            w.Write((byte) o.BonusType);
            w.Write(o.BonusId);
        }

        private void WriteWitchHut(BinaryWriter w, WitchHutObject o)
        {
            if (o.AllowedSkills != null)
            {
                WriteSkills(w, o.AllowedSkills);
            }
        }

        private void WriteSeerHot(BinaryWriter w, SeerHutObject o)
        {
            WriteQuest(w, o.Quest);
            if (o.Reward != null)
            {
                WriteReward(w, o.Reward);
            }
        }

        private void WriteReward(BinaryWriter w, QuestReward o)
        {
            w.Write((byte) o.Type);
            w.Write(o.Value);
            w.Write((sbyte) o.LuckMorale);
            w.Write((byte) o.Resource);
            w.Write((byte) o.SkillType);
            if (o.SecondarySkill != null)
            {
                w.Write((byte) o.SecondarySkill.Level);
                w.Write((byte) o.SecondarySkill.Type);
            }

            if (o.Artifact != null)
            {
                w.Write(o.Artifact.Value);
            }

            if (o.Spell != null)
            {
                w.Write(o.Spell.Value);
            }

            if (o.Monster != null)
            {
                w.Write(o.Monster.Value);
            }
        }

        private void WriteQuest(BinaryWriter w, QuestObject o)
        {
            w.Write((byte) o.Type);
            if (o.Skills != null)
            {
                WriteSkills(w, o.Skills);
            }

            if (o.Experience != null)
            {
                w.Write(o.Experience.Value);
            }

            if (o.ReferencedId != null)
            {
                w.Write(o.ReferencedId.Value);
            }

            if (o.Artifacts != null)
            {
                WriteArtifacts(w, o.Artifacts);
            }

            if (o.Creatues != null)
            {
                WriteCreatures(w, o.Creatues);
            }

            if (o.Resources != null)
            {
                WriteResources(w, o.Resources);
            }

            if (o.HeroId != null)
            {
                w.Write(o.HeroId.Value);
            }

            if (o.PlayerId != null)
            {
                w.Write((byte) o.PlayerId.Value);
            }

            if (o.Deadline != null)
            {
                w.Write(o.Deadline.Value);
            }

            if (o.FirstVisitText != null)
            {
                w.Write(o.FirstVisitText);
            }

            if (o.NextVisitText != null)
            {
                w.Write(o.NextVisitText);
            }

            if (o.CompletedText != null)
            {
                w.Write(o.CompletedText);
            }
        }

        private void WriteMonster(BinaryWriter w, MonsterObject o)
        {
            w.Write(o.Identifier);
            w.Write(o.Count);
            w.Write((byte) o.Disposition);
            if (o.Message != null)
            {
                w.Write(o.Message);
                WriteResources(w, o.Resources);
                if (o.Artifact != null)
                {
                    w.Write(o.Artifact.Value);
                }
            }

            w.Write(o.AlwaysAttacts);
            w.Write(o.KeepsSize);
        }

        private void WriteHero(BinaryWriter w, HeroObject o)
        {
            w.Write(o.Identifier);
            w.Write((byte) o.Owner);
            w.Write(o.SubId);
            if (o.Name != null)
            {
                w.Write(o.Name);
            }

            if (o.Experience.HasValue)
            {
                w.Write(o.Experience.Value);
            }

            if (o.PortraitId.HasValue)
            {
                w.Write(o.PortraitId.Value);
            }

            if (o.SecondarySkills != null)
            {
                WriteSkills(w, o.SecondarySkills);
            }

            if (o.Army != null)
            {
                WriteCreatures(w, o.Army);
            }

            w.Write((byte) o.ArmyFormationType);
            if (o.Inventory != null)
            {
                WriteInventory(w, o.Inventory);
            }

            w.Write((byte) o.PatrolRadius);
            if (o.Bio != null)
            {
                w.Write(o.Bio);
            }

            w.Write((byte) o.Sex);
            w.Write(o.StartsWithCustomSpell);
            if (o.Spells != null)
            {
                WriteSpells(w, o.Spells);
            }

            if (o.PrimarySkills != null)
            {
                WriteSkills(w, o.PrimarySkills);
            }
        }

        private void WriteEvent(BinaryWriter w, EventObject o)
        {
            WriteMessageAndGuards(w, o);
            w.Write(o.GainedExperience);
            w.Write(o.ManaDifference);
            w.Write((sbyte) o.MoraleDifference);
            w.Write((sbyte) o.LuckDifference);
            WriteResources(w, o.Resources);
            WriteSkills(w, o.PrimarySkills);
            WriteSkills(w, o.SecondarySkills);
            WriteArtifacts(w, o.Artifacts);
            WriteSpells(w, o.Spells);
            WriteCreatures(w, o.Monsters);
            w.Write((byte) o.CanBeTriggeredByPlayers);
            w.Write(o.CanBeTriggeredByAI);
            w.Write(o.CancelAfterFirstVisit);
        }

        private void WriteMessageAndGuards(BinaryWriter w, GuardedObject o)
        {
            if (o.Message != null)
            {
                w.Write(o.Message);
            }

            if (o.Guards != null)
            {
                WriteCreatures(w, o.Guards);
            }
        }

        private void WriteCreatures(BinaryWriter w, MapMonster[] monsters)
        {
            foreach (var m in monsters)
            {
                if (m != null)
                {
                    w.Write(m.Count);
                    w.Write(m.Monster.Value);
                }
                else
                {
                    w.Write(0);
                }
            }
        }

        private void WriteTemplates(BinaryWriter writer, MapObjectTemplate[] templates)
        {
            foreach (var template in templates)
            {
                writer.Write((int) template.Id);
                writer.Write(template.SubId);
                writer.Write((byte) template.Type);
                writer.Write(template.AnimationFile);
                writer.Write(template.BlockPosition.ToString());
                writer.Write(template.VisitPosition.ToString());
                writer.Write((ushort) template.EditorMenuLocation);
                writer.Write((ushort) template.SupportedTerrainTypes);
                writer.Write(template.IsBackground);
            }
        }

        private void WriteRumors(BinaryWriter writer, MapRumor[] rumors)
        {
            foreach (var r in rumors.OrderBy(h => h.Name).ThenBy(h => h.Value))
            {
                writer.Write(r.Name);
                writer.Write(r.Value);
            }
        }

        private void WriteSkills(BinaryWriter writer, SecondarySkillType[] skills)
        {
            foreach (var skill in skills.OrderBy(h => h))
            {
                writer.Write((byte) skill);
            }
        }

        private void WriteSpells(BinaryWriter writer, Identifier[] spells)
        {
            foreach (var spell in spells.OrderBy(h => h.Value))
            {
                writer.Write(spell.Value);
            }
        }

        private void WriteArtifacts(BinaryWriter writer, Identifier[] artifacts)
        {
            foreach (var artifact in artifacts.OrderBy(h => h.Value))
            {
                writer.Write(artifact.Value);
            }
        }

        private void WriteHeroes(BinaryWriter writer, MapHeroes heroes)
        {
            foreach (var hero in heroes.OrderBy(h => h.Id.Value))
            {
                writer.Write(hero.Id.Value);
                var cust = hero.Customisations;
                if (cust == null)
                {
                    continue;
                }

                if (cust.Name != null)
                {
                    writer.Write(cust.Name);
                }

                writer.Write((byte) cust.AllowedForPlayers);
                writer.Write(cust.PortraitId);
                if (cust.Experience.HasValue)
                {
                    writer.Write(cust.Experience.Value);
                }

                if (cust.SecondarySkills != null)
                {
                    WriteSkills(writer, cust.SecondarySkills);
                }

                if (cust.Inventory != null)
                {
                    WriteInventory(writer, cust.Inventory);
                }

                if (cust.Bio != null)
                {
                    writer.Write(cust.Bio);
                }

                writer.Write((byte) cust.Sex);
                if (cust.Spells != null)
                {
                    WriteSpells(writer, cust.Spells);
                }

                if (cust.PrimarySkills != null)
                {
                    WriteSkills(writer, cust.PrimarySkills);
                }
            }
        }

        private void WriteSkills(BinaryWriter writer, IDictionary<PrimarySkillType, int> skills)
        {
            foreach (var pair in skills.OrderBy(x => x.Key))
            {
                writer.Write((byte) pair.Key);
                writer.Write(pair.Value);
            }
        }

        private void WriteInventory(BinaryWriter writer, HeroArtifact[] inventory)
        {
            foreach (var artifact in inventory
                .Where(x => x != null)
                .OrderBy(x => x.Slot)
                .ThenBy(x => x.Artifact.Value))
            {
                writer.Write((int) artifact.Slot);
                writer.Write(artifact.Artifact.Value);
            }
        }

        private void WriteSkills(BinaryWriter writer, SecondarySkill[] skills)
        {
            foreach (var skill in skills.OrderBy(x => x.Type))
            {
                writer.Write((byte) skill.Type);
                writer.Write((byte) skill.Level);
            }
        }

        private void WriteHeader(BinaryWriter writer, MapInfo mapInfo)
        {
            writer.Write((int) mapInfo.Format);
            writer.Write((byte) mapInfo.FormatSubversion);
            writer.Write((byte) mapInfo.Difficulty);
            writer.Write(mapInfo.ExperienceLevelLimit);
            writer.Write(mapInfo.AllowSpecialWeeks);
            WriteVictorConditions(writer, mapInfo.VictoryCondition);
            WriteLossCondition(writer, mapInfo.LossCondition);
            WritePlayers(writer, mapInfo.Players);
        }

        private void WritePlayers(BinaryWriter writer, MapPlayer[] players)
        {
            foreach (var player in players)
            {
                writer.Write(player.CanAIPlay);
                writer.Write(player.CanHumanPlay);
                writer.Write(player.TeamId.GetValueOrDefault(-1));
                writer.Write((byte) player.AITactic);
                writer.Write(player.AllowedAlignmentsCustomised);
                writer.Write((int) player.AllowedFactions);
                writer.Write(player.IsFactionRandom);
                writer.Write(player.HasMainTown);
                if (player.HasMainTown)
                {
                    writer.Write(player.GenerateHeroAtMainTown);
                    // main town type will be determined when we write out the town
                    WritePosition(writer, player.MainTownPosition);
                }

                // TODO: Random hero and hero placeholder ? Or we don't care here?
                // we can either do it here or with map objects I guess...
            }
        }

        private void WriteLossCondition(BinaryWriter writer, LossCondition lc)
        {
            writer.Write((byte) lc.Type);
            if (lc.Type == LossConditionType.LossStandard)
            {
                return;
            }

            switch (lc.Type)
            {
                case LossConditionType.LossCastle:
                case LossConditionType.LossHero:
                    WritePosition(writer, lc.Position);
                    break;
                case LossConditionType.TimeExpires:
                    writer.Write(lc.Value);
                    break;
            }
        }

        private void WriteVictorConditions(BinaryWriter writer, VictoryCondition vc)
        {
            writer.Write((byte) vc.Type);
            if (vc.Type == VictoryConditionType.WinStandard) return;
            writer.Write(vc.AllowNormalVictory);
            writer.Write(vc.AppliesToAI);
            switch (vc.Type)
            {
                case VictoryConditionType.Artifact:
                    writer.Write(vc.Identifier.Value);
                    break;
                case VictoryConditionType.GatherTroop:
                    writer.Write(vc.Identifier.Value);
                    writer.Write(vc.Value);
                    break;
                case VictoryConditionType.GatherResource:
                    writer.Write((byte) vc.Resource);
                    writer.Write(vc.Value);
                    break;
                case VictoryConditionType.BuildCity:
                    WritePosition(writer, vc.Position);
                    writer.Write((byte) vc.HallLevel);
                    writer.Write((byte) vc.CastleLevel);
                    break;
                case VictoryConditionType.BuildGrail:
                    if (vc.Position != null)
                        WritePosition(writer, vc.Position);
                    break;
                case VictoryConditionType.BeatHero:
                case VictoryConditionType.CaptureCity:
                case VictoryConditionType.BeatMonster:
                    WritePosition(writer, vc.Position);
                    break;
                case VictoryConditionType.TransportItem:
                    writer.Write(vc.Identifier.Value);
                    WritePosition(writer, vc.Position);
                    break;
                case VictoryConditionType.TakeDwellings:
                case VictoryConditionType.TakeMines:
                    break;
                case VictoryConditionType.BeatAllMonsters: // HotA
                    break;
                case VictoryConditionType.Survive: // HotA
                    writer.Write(vc.Value);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void WritePosition(BinaryWriter writer, MapPosition position)
        {
            writer.Write(position.X);
            writer.Write(position.Y);
            writer.Write(position.Z);
        }

        private static void WriteTerrain(BinaryWriter writer, MapTerrain terrain)
        {
            foreach (var row in terrain.Ground)
            {
                foreach (var tile in row)
                {
                    writer.Write((byte) tile.TerrainType);
                    if (tile.RiverType != RiverType.NoRiver)
                    {
                        writer.Write((byte) tile.RiverType);
                        writer.Write((byte) tile.RiverDirection);
                    }

                    if (tile.RoadType != RoadType.NoRoad)
                    {
                        writer.Write((byte) tile.RoadType);
                        writer.Write((byte) tile.RoadDirection);
                    }

                    writer.Write((byte) tile.DisplayOptions);
                }
            }

            if (terrain.Undrground != null)
            {
                foreach (var row in terrain.Undrground)
                {
                    foreach (var tile in row)
                    {
                        writer.Write((byte) tile.TerrainType);
                    }
                }
            }
        }

        public void Dump(ILogger logger)
        {
            Log.Information("Total files: {total}, unique {unique}", totalCount, hashes.Count);
            foreach (var g in hashes)
            {
                if (g.Count() > 1)
                {
                    logger.Information("Duplicated files: {@files}", g);
                }
            }
        }

        public void Dispose()
        {
            sha1Managed?.Dispose();
            totalCount = 0;
        }
    }
}