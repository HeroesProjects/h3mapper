using H3Mapper.Flags;
using Serilog;

namespace H3Mapper.MapObjects
{
    public class WoGObject : MapObject
    {
        public WoGObject(int rawId)
        {
            if (rawId > 75)
            {
                Log.Warning(
                    "Unexpected WoG object id {wogObjectId} for enum type {enumType}", rawId);
            }
            Type = (WoGObjectType) rawId;
        }

        public WoGObjectType Type { get; }
    }
}