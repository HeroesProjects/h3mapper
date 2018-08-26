using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class ScholarObject : MapObject
    {
        public ScholarBonusType BonusType { get; set; }
        public PrimarySkillType? PrimarySkill { get; set; }
        public SecondarySkillType? SecondarySkill { get; set; }
        public Identifier Spell { get; set; }
    }
}