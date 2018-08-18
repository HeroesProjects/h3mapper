using H3Mapper.Flags;
using H3Mapper.MapObjects;
using Serilog;

namespace H3Mapper.Analysis
{
    public class MapObjectSubIdValidator : IMapValidator
    {
        public void Validate(H3Map map)
        {
            foreach (var mapObject in map.Objects)
            {
                CheckForUnexpectedTemplateSubId(mapObject);
            }
        }

        private void CheckForUnexpectedTemplateSubId(MapObject mo)
        {
            switch (mo.Template.Id)
            {
                // those appear with non-0 ids in map object specifications
                case ObjectId.Artifact:
                case ObjectId.Boat:
                case ObjectId.BorderGuard:
                case ObjectId.KeymastersTent:
                case ObjectId.Cartographer:
                case ObjectId.CreatureBank:
                case ObjectId.CreatureGenerator1:
                case ObjectId.CreatureGenerator4:
                case ObjectId.Garrison:
                case ObjectId.Hero:
                case ObjectId.HillFort:
                case ObjectId.IdolOfFortune:
                case ObjectId.DecorativeObject:
                case ObjectId.LibraryOfEnlightenment:
                case ObjectId.MonolithOneWayEntrance:
                case ObjectId.MonolithOneWayExit:
                case ObjectId.MonolithTwoWay:
                case ObjectId.SchoolOfMagic:
                case ObjectId.MagicWell:
                case ObjectId.Mine:
                case ObjectId.Monster:
                case ObjectId.RedwoodObservatory:
                case ObjectId.Object:
                case ObjectId.Resource:
                case ObjectId.SeersHut:
                case ObjectId.ShrineOfMagicIncantation:
                case ObjectId.ShrineOfMagicThought:
                case ObjectId.Tavern:
                case ObjectId.Town:
                case ObjectId.LearningStone:
                case ObjectId.TreasureChest:
                case ObjectId.SubterraneanGate:
                case ObjectId.WarMachineFactory:
                case ObjectId.WitchHut:
                case ObjectId.DecorativeObject2:
                case ObjectId.DecorativeObject3:
                case ObjectId.MagicalTerrain:
                case ObjectId.ResourceWarehouse:
                case ObjectId.Building:
                case ObjectId.SeaObject:
                case ObjectId.Building2:
                case ObjectId.DirtHills:
                case ObjectId.GrassHills:
                case ObjectId.BorderGate:
                case ObjectId.RandomDwellingLevel:
                case ObjectId.RandomDwellingFaction:
                case ObjectId.Garrison2:
                case ObjectId.Mine2:
                    return;
                // not real objects. Those should never appear
                case ObjectId.AnchorPoint: // WTF even is Anchor Point?
                case ObjectId.CreatureGenerator2:
                case ObjectId.CreatureGenerator3:
                    LogUnexpectedObject(mo);
                    return;
                default:
                    if (mo.Template.SubId > 0)
                    {
                        LogUnexpectedType(mo);
                    }

                    return;
            }
        }

        private void LogUnexpectedObject(MapObject mo)
        {
            Log.Warning("Unexpected Object {id}/{subid}:{type} {animationFile} {location}",
                mo.Template.Id,
                mo.Template.SubId,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }

        private static void LogUnexpectedType(MapObject mo)
        {
            Log.Information(
                "Unexpected Object Subtype {subid} for object {id}:{type} {animationFile} {location}",
                mo.Template.SubId,
                mo.Template.Id,
                mo.Template.Type,
                mo.Template.AnimationFile,
                mo.Position);
        }
    }
}