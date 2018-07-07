using H3Mapper.Flags;

namespace H3Mapper
{
    public class MapObjectTemplate
    {
        public string AnimationFile { get; set; }
        public Terrains SupportedTerrainTypes { get; set; }
        public ObjectId Id { get; set; }
        public int SubId { get; set; }
        public ObjectType Type { get; set; }
        public int PrintPriority { get; set; }
        public Terrains SupportedTerrainTypes2 { get; set; }
    }
}