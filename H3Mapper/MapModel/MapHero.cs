using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class MapHero
    {
        public Identifier Id { get; set; }
        public int PortraitId { get; set; }
        public string Name { get; set; }
        public Players AllowedForPlayers { get; set; }
        public int? Experience { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public HeroArtifact[] Inventory { get; set; }
        public string Bio { get; set; }
        public HeroSex Sex { get; set; }
        public Identifier[] Spells { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
    }
}