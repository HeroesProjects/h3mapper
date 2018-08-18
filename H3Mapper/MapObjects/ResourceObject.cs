using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class ResourceObject : GuardedObject
    {
        public long Amount { get; set; }
        public Resource Resource { get; set; }
    }
}