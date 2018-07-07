using H3Mapper.Flags;

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
        public Player Alignment { get; set; }
        public Identifier[] SpellsWillAppear { get; set; }
        public Identifier[] SpellsMayAppear { get; set; }
    }
}