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
        public TerrainMenus EditorMenuLocation { get; set; }
        public Position BlockPosition { get; set; }
        public Position VisitPosition { get; set; }
        public bool IsBackground { get; set; }
    }
}