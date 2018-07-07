using System;

namespace H3Mapper.Flags
{
    [Flags]
    public enum TileMirroring : byte
    {
        None,
        GroundMirroredVertically = 1 << 0,
        GroundMirroredHorizontally = 1 << 1,
        RiverMirroredVertically = 1 << 2,
        RiverMirroredHorizontally = 1 << 3,
        RoadMirroredVertically = 1 << 4,
        RoadMirroredHorizontally = 1 << 5,
    }
}