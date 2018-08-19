namespace H3Mapper.Flags
{
    public enum RoadDirection : byte
    {
        // values correspond to frames in cobbrd.def (for cobbled road, and respective files for other types) 
        Bend1 = 0,
        Bend2 = 1,
        Bend3 = 2,
        Bend4 = 3,
        Bend5 = 4,
        Bend6 = 5,
        SplitThreeWaysVertically1 = 6,
        SplitThreeWaysVertically2 = 7,
        SplitThreeWaysHorizontally1 = 8,
        SplitThreeWaysHorizontally2 = 9,
        Vertical1 = 10,
        Vertical2 = 11,
        Horizontal1 = 12,
        Horizontal2 = 13,
        VerticalShort = 14,
        HorizontalShort = 15,
        SplitFourWays = 16,
    }
}