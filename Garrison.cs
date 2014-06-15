namespace H3Mapper
{
    public class Garrison:MapObject
    {
        public Player Owner { get; set; }
        public MapCreature[] Creatues { get; set; }
        public bool UnitsAreRemovable { get; set; }
    }
}