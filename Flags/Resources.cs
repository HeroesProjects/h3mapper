using System;

namespace H3Mapper.Flags
{
    [Flags]
    public enum Resources
    {
        None = 0,
        Wood = 1 << 0,
        Mercury = 1 << 1,
        Ore = 1 << 2,
        Sulfur = 1 << 3,
        Crystal = 1 << 4,
        Gems = 1 << 5,
        Gold = 1 << 6
    }
}