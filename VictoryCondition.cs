using H3Mapper.Flags;

namespace H3Mapper
{
    public class VictoryCondition
    {
        public VictoryConditionType Type { get; set; }
        public bool AllowNormalVictory { get; set; }
        public bool AppliesToAI { get; set; }
        public int ObjectType { get; set; }
        public int Value { get; set; }
        public MapPosition Position { get; set; }
        public BuildingLevel HallLevel { get; set; }
        public BuildingLevel CastleLevel { get; set; }

        public override string ToString()
        {
            return
                string.Format(
                    "Type: {0}, AllowNormalVictory: {1}, AppliesToAI: {2}, ObjectType: {3}, Value: {4}, Position: {5}, HallLevel: {6}, CastleLevel: {7}",
                    Type, AllowNormalVictory, AppliesToAI, ObjectType, Value, Position, HallLevel, CastleLevel);
        }
    }
}