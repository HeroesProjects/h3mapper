using H3Mapper.Internal;

namespace H3Mapper.MapObjects
{
    public class MapObject
    {
        public MapObjectTemplate Template { get; set; }

        public MapPosition Position { get; set; }
        public string Message { get; set; }
        public MapMonster[] Guards { get; set; }
    }

    public class MapObject<TTypeEnum> : MapObject where TTypeEnum : struct
    {
        public MapObject(int typeRawValue)
        {
            Type = EnumValues.Cast<TTypeEnum>(typeRawValue);
        }

        public TTypeEnum Type { get; set; }
    }
}