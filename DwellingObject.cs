namespace H3Mapper
{
    public class DwellingObject : MapObject
    {
        public Player Player { get; set; }
        public Factions AllowedFactions { get; set; }
        public UnitLevel MinLevel { get; set; }
        public UnitLevel MaxLevel { get; set; }
        public uint SameAsCastle { get; set; }
    }
}