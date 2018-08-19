using H3Mapper.Flags;

namespace H3Mapper
{
    public class MapTile
    {
        public MapTile(int x, int y, int z)
        {
            Location = new MapPosition {X = x, Y = y, Z = z};
        }

        public MapPosition Location { get; set; }
        public Terrain TerrainType { get; set; }
        public int TerrainVariant { get; set; }
        public RiverType RiverType { get; set; }
        public RiverDirection RiverDirection { get; set; }
        public RoadType RoadType { get; set; }
        public RoadDirection RoadDirection { get; set; }
        public TileMirroring DisplayOptions { get; set; }
    }
}