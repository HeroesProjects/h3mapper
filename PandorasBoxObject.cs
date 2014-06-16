using System.Collections.Generic;

namespace H3Mapper
{
    public class PandorasBoxObject : MapObject
    {
        public long GainedExperience { get; set; }
        public long ManaDifference { get; set; }
        public int MoraleDifference { get; set; }
        public int LuckDifference { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public Artifact[] Artifacts { get; set; }
        public Spell[] Spells { get; set; }
        public MapCreature[] Creatures { get; set; }
    }
}