namespace H3Mapper.Flags
{
    public enum LuckMoraleModifier : byte
    {
        None,
        Plus1,
        Plus2,
        Plus3,
        Minus1 = byte.MaxValue,
        Minus2 = byte.MaxValue - 1,
        Minus3 = byte.MaxValue - 2
    }
}