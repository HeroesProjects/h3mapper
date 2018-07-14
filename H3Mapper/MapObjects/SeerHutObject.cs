using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class SeerHutObject : MapObject
    {
        public QuestObject Quest { get; set; }
        public QuestReward Reward { get; set; }
        public SeerHutType Type { get; set; }
    }
}