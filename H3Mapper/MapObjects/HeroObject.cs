using System.Collections.Generic;
using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class HeroObject : MapObject
    {
        public bool StartsWithCustomSpell { get; set; }
        public long Identifier { get; set; }
        public Player Owner { get; set; }
        public Identifier Identity { get; set; }
        public long? Experience { get; set; }
        public string Name { get; set; }
        public int? PortraitId { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public MapMonster[] Army { get; set; }
        public Formation ArmyFormationType { get; set; }
        public HeroArtifact[] Inventory { get; set; }
        public PatrolRadius PatrolRadius { get; set; }
        public string Bio { get; set; }
        public HeroSex Sex { get; set; }
        public Identifier[] Spells { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public HeroType? Type { get; set; }
    }
}