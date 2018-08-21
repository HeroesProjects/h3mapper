using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class LossCondition
    {
        public LossConditionType Type { get; set; }
        public MapPosition Position { get; set; }
        public int Value { get; set; }
    }
}