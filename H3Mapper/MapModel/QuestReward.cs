using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class QuestReward
    {
        public RewardType Type { get; set; }
        public int Value { get; set; }
        public Resource Resource { get; set; }
        public PrimarySkillType SkillType { get; set; }
        public Identifier Artifact { get; set; }
        public SecondarySkill SecondarySkill { get; set; }
        public Identifier Monster { get; set; }
        public Identifier Spell { get; set; }
        public LuckMoraleModifier LuckMorale { get; set; }
    }
}