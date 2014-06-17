using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper
{
    public class MapHeroDefinition
    {
        public int HeroId { get; set; }
        public int Experience { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public HeroArtifact[] Inventory { get; set; }
        public string Bio { get; set; }
        public HeroSex Sex { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public Identifier[] Spells { get; set; }
    }
}