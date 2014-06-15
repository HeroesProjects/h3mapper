namespace H3Mapper
{
    public enum MapFormat
    {
        Empty = 0,
        RoE = 0x0e, // 14
        AB = 0x15, // 21
        SoD = 0x1c, // 28
        HotA1 = 0x1e,
        HotA2 = 0x1f,
        HotA3 = 0x20,
        HotA = HotA1 | HotA2 | HotA3,
        WoG = 0x33 // 
    }
}