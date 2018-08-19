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
        /// <summary>
        /// <remarks>
        /// I'm not sure if that's exactly what it means but from my observations the bit is set for all non-water
        /// fields that are adjacent to water. That value can also be inferred just by looking at the surrounding tiles
        /// so I'm not sure why they bothered putting it here... but here it is nonetheless
        /// </remarks>
        /// </summary>
        IsBorderingWater = 1 << 6,
    }
}