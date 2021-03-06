using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class TownObject : MapObject
    {
        public long Identifier { get; set; }
        public Player Owner { get; set; }
        public string Name { get; set; }
        public MapMonster[] Garrison { get; set; }
        public Formation GarrisonFormation { get; set; }
        public bool[] BuiltBuildingIds { get; set; }
        public bool[] ForbiddenBuildingIds { get; set; }
        public bool HasFort { get; set; }
        public TimedEvents[] Events { get; set; }
        public RandomTownAlignment Alignment { get; set; }
        public Identifier[] SpellsThatMustAppear { get; set; }
        public Identifier[] SpellsThatMayAppear { get; set; }
        public Faction Faction { get; set; }
        public bool AllowSpellResearch { get; set; } // HotA only
    }
}