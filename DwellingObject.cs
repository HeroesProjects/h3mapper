namespace H3Mapper
{
    public class DwellingObject:MapObject
    {
        public Player Player { get; set; }
        public Factions AllowedFactions { get; set; }
        public byte Castles0 { get; set; }
        public byte Castles1 { get; set; }
        public Player Owner { get; set; }
        public UnitLevel MinLevel { get; set; }
        public UnitLevel MaxLevel { get; set; }
    }
}