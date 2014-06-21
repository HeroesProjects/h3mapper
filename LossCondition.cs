using H3Mapper.Flags;

namespace H3Mapper
{
    public class LossCondition
    {
        public LossConditionType Type { get; set; }
        public MapPosition Position { get; set; }
        public int Value { get; set; }
    }
}