namespace H3Mapper.Flags
{
    public enum VictoryConditionType : byte
    {
        Artifact,
        GatherTroop,
        GatherResource,
        BuildCity,
        BuildGrail,
        BeatHero,
        CaptureCity,
        BeatMonster,
        TakeDwellings,
        TakeMines,
        TransportItem,
        BeatAllMonsters,
        Survive,
        WinStandard = byte.MaxValue
    }
}