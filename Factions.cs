using System;

namespace H3Mapper
{
    [Flags]
    public enum Factions:ushort
    {
        None = 0,
        Castle = 1 << 0,
        Rampart = 1 << 1,
        Tower = 1 << 2,
        Inferno = 1 << 3,
        Necropolis = 1 << 4,
        Dungeon = 1 << 5,
        Stronghold = 1 << 6,
        Fortress = 1 << 7,
        Conflux = 1 << 8,
        Custom1 = 1 << 9,
        Custom2 = 1 << 10,
        Custom3 = 1 << 11,
        Custom4 = 1 << 12,
        Cove = Custom1
    }
}