namespace H3Mapper
{
    public class MapTile
    {
        public MapTile(int x, int y, int z)
        {
            Location = new MapPosition {X = x, Y = y, Z = z};
        }

        public MapPosition Location { get; set; }
        public int TerrainType { get; set; }
        public int TerrainView { get; set; }
        public int RiverType { get; set; }
        public int RiverDirection { get; set; }
        public int RoadType { get; set; }
        public int RoadDirection { get; set; }
        public int Flags { get; set; }
    }
}