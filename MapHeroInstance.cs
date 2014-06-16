using System.Collections.Generic;

namespace H3Mapper
{
    public class MapHeroInstance : MapObject
    {
        public long Indentifier { get; set; }
        public Player Owner { get; set; }
        public int SubId { get; set; }
        public long? Experience { get; set; }
        public string Name { get; set; }
        public int? PortraitId { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public MapCreature[] Army { get; set; }
        public Formation ArmyFormationType { get; set; }
        public HeroArtifact[] Inventory { get; set; }
        public int? PatrolRadius { get; set; }
        public string Bio { get; set; }
        public HeroSex? Sex { get; set; }
        public Spell[] Spells { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
    }
}