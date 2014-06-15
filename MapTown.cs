namespace H3Mapper
{
    public class MapTown:MapObject
    {
        public long Identifier { get; set; }
        public Player Owner { get; set; }
        public string Name { get; set; }
        public MapCreature[] Garrison { get; set; }
        public Formation GarrisonFormation { get; set; }
        public bool[] BuiltBuildingIds { get; set; }
        public bool[] ForbiddenBuildingIds { get; set; }
        public bool HasFort { get; set; }
        public TimedEvents[] Events { get; set; }
        public int Alignment { get; set; }
    }
}