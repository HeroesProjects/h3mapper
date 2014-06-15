namespace H3Mapper
{
    public class LossCondition
    {
        public LossConditionType Type { get; set; }
        public MapPosition Position { get; set; }
        public int Value { get; set; }

        public override string ToString()
        {
            return string.Format("Type: {0}, Position: {1}, Value: {2}", Type, Position, Value);
        }
    }
}