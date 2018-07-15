using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class GarrisonObject : MapObject<GarrisonType>
    {
        public GarrisonObject(int typeRawValue) : base(typeRawValue)
        {
        }

        public Player Owner { get; set; }
        public MapMonster[] Creatues { get; set; }
        public bool UnitsAreRemovable { get; set; }
        public GarrisonOrientation Orientation { get; set; }
    }
}