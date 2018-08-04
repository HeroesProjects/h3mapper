using H3Mapper.Flags;
using H3Mapper.MapObjects;
using Serilog;

namespace H3Mapper.Analysis
{
    public class TemplateValidator
    {
        public void Validate(H3Map map)
        {
            foreach (var mapObject in map.Objects)
            {
                CheckForUnexpectedTemplateSubId(mapObject, map.Info);
            }
        }

        private void CheckForUnexpectedTemplateSubId(MapObject mo, MapInfo info)
        {
            switch (mo.Template.Id)
            {
                // objects that have no customisable behaviour or variants
                case ObjectId.IdolOfFortune:
                case ObjectId.LibraryOfEnlightenment:
                case ObjectId.MagicWell:
                    if (mo.Template.SubId > 1)
                    {
                        LogUnexpectedType(mo);
                    }

                    if (mo.Template.SubId == 1)
                    {
                        RequireVersion(mo, info, MapFormat.WoG);
                    }

                    return;

                case ObjectId.Object:
                case ObjectId.TreasureChest:
                    if (mo.Template.SubId > 1)
                    {
                        RequireVersion(mo, info, MapFormat.WoG);
                    }

                    return;
                case ObjectId.SchoolOfMagic:
                    if (mo.Template.SubId > 1)
                    {
                        LogUnexpectedType(mo);
                    }

                    if (mo.Template.SubId == 1)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }

                    return;
                // objects that vary by version
                case ObjectId.MonolithOneWayEntrance:
                case ObjectId.MonolithOneWayExit:
                    var m = (MapObject<MonolithOneWayType>) mo;
                    if (m.Type >= MonolithOneWayType.Turquoise)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }
                    else if (m.Type >= MonolithOneWayType.Yellow)
                    {
                        RequireVersionAtLeast(mo, info, MapFormat.SoD);
                    }

                    return;
                case ObjectId.MonolithTwoWay:
                    var m2 = (MapObject<MonolithTwoWayType>) mo;
                    if (m2.Type >= MonolithTwoWayType.WhiteSeaPortal)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }
                    else if (m2.Type >= MonolithTwoWayType.Orange)
                    {
                        RequireVersionAtLeast(mo, info, MapFormat.SoD);
                    }

                    return;
                case ObjectId.SeersHut:
                    var m3 = (SeerHutObject) mo;
                    if (m3.Type >= SeerHutType.Water)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }

                    return;

                case ObjectId.CreatureBank:
                    var m4 = (MapObject<CreatureBankType>) mo;
                    if (m4.Type >= CreatureBankType.BeholdersSanctuary)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }
                    else if (m4.Type >= CreatureBankType.HuntingLodge)
                    {
                        RequireVersion(mo, info, MapFormat.WoG);
                    }
                    return;

                // not real objects. Those should never appear
                case ObjectId.AnchorPoint: // WTF even is Anchor Point?
                case ObjectId.CreatureGenerator2:
                case ObjectId.CreatureGenerator3:
                    LogUnexpectedObject(mo);
                    return;

                // Objects that are validated during read
                case ObjectId.Artifact:
                case ObjectId.BorderGuard:
                case ObjectId.KeymastersTent:
                case ObjectId.Cartographer:
                case ObjectId.CreatureGenerator1:
                case ObjectId.CreatureGenerator4:
                case ObjectId.Event:
                case ObjectId.Garrison:
                case ObjectId.Hero:
                case ObjectId.Mine:
                case ObjectId.Monster:
                case ObjectId.Prison:
                case ObjectId.RandomArtifact:
                case ObjectId.RandomTreasureArtifact:
                case ObjectId.RandomMinorArtifact:
                case ObjectId.RandomMajorArtifact:
                case ObjectId.RandomRelicArtifact:
                case ObjectId.RandomHero:
                case ObjectId.RandomMonster:
                case ObjectId.RandomMonster1:
                case ObjectId.RandomMonster2:
                case ObjectId.RandomMonster3:
                case ObjectId.RandomMonster4:
                case ObjectId.RandomResource:
                case ObjectId.RandomTown:
                case ObjectId.RandomDwelling:
                case ObjectId.RandomDwellingLevel:
                case ObjectId.RandomDwellingFaction:
                case ObjectId.Resource:
                case ObjectId.ShrineOfMagicIncantation:
                case ObjectId.ShrineOfMagicGesture:
                case ObjectId.ShrineOfMagicThought:
                case ObjectId.BorderGate:
                case ObjectId.Town:
                case ObjectId.RandomMonster5:
                case ObjectId.RandomMonster6:
                case ObjectId.RandomMonster7:
                case ObjectId.HeroPlaceholder:
                case ObjectId.Garrison2:
                case ObjectId.Mine2:
                    return;

                case ObjectId.Boat:
                    // TODO: only 2 noticed so far
                    if (mo.Template.SubId >= 3)
                    {
                        RequireVersion(mo, info, MapFormat.HotA);
                    }

                    if (mo.Template.SubId >= 6)
                    {
                        LogUnexpectedObject(mo);
                    }

                    return;
                case ObjectId.DecorativeObject:
                case ObjectId.DecorativeObject2:
                case ObjectId.DecorativeObject3:
                    // TODO: commented out temporarily to avoid noise
//                    RequireVersion(mo, info, MapFormat.WoG);
                    return;

                default:
                    if (mo.Template.SubId > 0)
                    {
                        LogUnexpectedType(mo);
                    }

                    return;
            }
        }

        private void LogExpectedObject(MapObject mo)
        {
            Log.Information("# Object {id}/{subid}:{type} {animationFile} {location}",
                mo.Template.Id,
                mo.Template.SubId,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }

        private void LogUnexpectedObject(MapObject mo)
        {
            Log.Warning("# Unexpected Object {id}/{subid}:{type} {animationFile} {location}",
                mo.Template.Id,
                mo.Template.SubId,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }

        private static void LogUnexpectedType(MapObject mo)
        {
            Log.Information("# Unexpected Object Subtype {subid} for object {id}:{type} {animationFile} {location}",
                mo.Template.SubId,
                mo.Template.Id,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }

        private static void RequireVersionAtLeast(MapObject mo, MapInfo info, MapFormat version)
        {
            if (info.Format >= version) return;

            Log.Information(
                "# Unexpected Object {id}/{subId}:{type} {animationFile} {location}. " +
                "Required version at least {requiredVersion} but is {actualVersion}",
                mo.Template.Id,
                mo.Template.SubId,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position,
                version,
                info.Format);
        }

        private static void RequireVersion(MapObject mo, MapInfo info, MapFormat version)
        {
            if (info.Format == version) return;
            Log.Information(
                "# Unexpected Object {id}/{subId}:{type} {animationFile} {location}. " +
                "Required version {requiredVersion} but is {actualVersion}",
                mo.Template.Id,
                mo.Template.SubId,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position,
                version,
                info.Format);
        }
    }
}