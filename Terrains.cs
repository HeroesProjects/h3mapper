using System;

namespace H3Mapper
{
    [Flags]
    public enum Terrains : ushort
    {
        None = ushort.MaxValue,
        Water = 1 << 0,
        Lava = 1 << 1,
        Underground = 1 << 2,
        Stones = 1 << 3,
        Swamp = 1 << 4,
        Snow = 1 << 5,
        Grass = 1 << 6,
        Sand = 1 << 7,
        Dirt = 1 << 8,
        Custom1 = 1 << 9,
        Custom2 = 1 << 10,
        Custom3 = 1 << 11,
        Custom4 = 1 << 12
    }
}