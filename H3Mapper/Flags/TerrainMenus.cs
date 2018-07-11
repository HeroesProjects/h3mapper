using System;

namespace H3Mapper.Flags
{
    [Flags]
    public enum TerrainMenus : ushort
    {
        Dirt = 1 << 0,
        Sand = 1 << 1,
        Grass = 1 << 2,
        Snow = 1 << 3,
        Swamp = 1 << 4,
        Rough = 1 << 5,
        Subterranean = 1 << 6,
        Lava = 1 << 7,
        Water = 1 << 8,
        All = 1 << 9
    }
}