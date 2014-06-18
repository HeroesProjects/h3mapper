using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class GarrisonObject : MapObject
    {
        public Player Owner { get; set; }
        public MapMonster[] Creatues { get; set; }
        public bool UnitsAreRemovable { get; set; }
    }
}