namespace H3Mapper.Flags
{
    public enum RandomTownAlignment : byte
    {
        SameAsRed = 0,
        SameAsBlue,
        SameAsTan,
        SameAsGreen,
        SameAsOrange,
        SameAsPurple,
        SameAsTeal,
        SameAsPink,
        DifferentFromRed,
        DifferentFromBlue,
        DifferentFromTan,
        DifferentFromGreen,
        DifferentFromOrange,
        DifferentFromPurple,
        DifferentFromTeal,
        DifferentFromPink,
        
        SameAsOwnerOrRandom = byte.MaxValue
    }
}