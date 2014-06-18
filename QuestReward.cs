using H3Mapper.Flags;

namespace H3Mapper
{
    public class QuestReward
    {
        public RewardType Type { get; set; }
        public long Value { get; set; }
        public int Modifier { get; set; }
        public Resource ResourceType { get; set; }
        public PrimarySkillType SkillType { get; set; }
        public int ItemId { get; set; }
        public Identifier Artifact { get; set; }
        public SecondarySkill SecondarySkill { get; set; }
        public Identifier Monster { get; set; }
    }
}