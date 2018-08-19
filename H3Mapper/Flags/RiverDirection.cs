namespace H3Mapper.Flags
{
    public enum RiverDirection : byte
    {
        // values correspond to frames in Clrrvr.def (for normal river, and respective files for other types) 
        Bend1 = 0,
        Bend2 = 1,
        Bend3 = 2,
        Bend4 = 3,
        SplitFourWays = 4,
        SplitThreeWaysHorizontally1 = 5,
        SplitThreeWaysHorizontally2 = 6,
        SplitThreeWaysVertically1 = 7,
        SplitThreeWaysVertically2 = 8,
        Vertical1 = 9,
        Vertical2 = 10,
        Horizontal1 = 11,
        Horizontal2 = 12
    }
}