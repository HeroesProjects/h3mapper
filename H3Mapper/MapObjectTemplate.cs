using System.Diagnostics;
using H3Mapper.Flags;

namespace H3Mapper
{
    [DebuggerDisplay("{Id}/{SubId}: {Type} ({AnimationFile})")]
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

        public override string ToString()
        {
            return $"{nameof(AnimationFile)}: {AnimationFile}, {nameof(Id)}: {Id}, {nameof(SubId)}: {SubId}, {nameof(Type)}: {Type}";
        }
    }
}