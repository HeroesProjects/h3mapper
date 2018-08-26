using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class DwellingObject : MapObject
    {
        public Player Player { get; set; }
        public Factions? AllowedFactions { get; set; }
        public UnitLevel MinLevel { get; set; }
        public UnitLevel MaxLevel { get; set; }
        public long? FactionSameAsTownId { get; set; }
        public Faction? RandomDwellingFaction { get; set; }
    }
}