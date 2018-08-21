using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class VictoryCondition
    {
        public VictoryConditionType Type { get; set; }
        public bool AllowNormalVictory { get; set; }
        public bool AppliesToAI { get; set; }
        public int Value { get; set; }
        public MapPosition Position { get; set; }
        public BuildingLevel HallLevel { get; set; }
        public BuildingLevel CastleLevel { get; set; }
        public Identifier Identifier { get; set; }
        public Resource Resource { get; set; }
    }
}