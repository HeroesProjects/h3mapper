using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class PlayerOwnedObject : MapObject
    {
        public Player Owner { get; set; }
    }
}