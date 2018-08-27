using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class MapInfo
    {
        public MapInfo()
        {
            AllowSpecialWeeks = true;
        }
        // Map Version
        public MapFormat Format { get; set; }
        // Difficulty
        public Difficulty Difficulty { get; set; }
        // Two level map
        public bool HasSecondLevel { get; set; }
        // Limit hero experience level to:
        public int? ExperienceLevelLimit { get; set; }
        // Allow monster/plague weeks
        public bool AllowSpecialWeeks { get; set; }
        // Arena
        public bool Arena { get; set; }
        // Map name:
        public string Name { get; set; }
        // Description:
        public string Description { get; set; }
        
        // On new map screen:
        // Map size
        public int Size { get; set; }
        
        // Implied properties:
        // Only in HotA. Not sure if this is depepdent on HotA version or something else
        public MapSubformat Subformat { get; set; }
        
        // true if there's at least one playable player on the map
        public bool HasPlayers { get; set; }
    }
}