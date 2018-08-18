using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class PandorasBoxObject : GuardedObject
    {
        public long GainedExperience { get; set; }
        public long ManaDifference { get; set; }
        public LuckMoraleModifier MoraleDifference { get; set; }
        public LuckMoraleModifier LuckDifference { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public Identifier[] Artifacts { get; set; }
        public Identifier[] Spells { get; set; }
        public MapMonster[] Monsters { get; set; }
    }
}