using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class ResourceObject : MapObject
    {
        public long Amount { get; set; }
        public Resource Resource { get; set; }
    }
}