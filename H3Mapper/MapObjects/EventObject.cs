using System.Collections.Generic;
using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class EventObject : GuardedObject
    {
        public int GainedExperience { get; set; }
        public int ManaDifference { get; set; }
        public LuckMoraleModifier MoraleDifference { get; set; }
        public LuckMoraleModifier LuckDifference { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public Identifier[] Artifacts { get; set; }
        public Identifier[] Spells { get; set; }
        public MapMonster[] Monsters { get; set; }
        public Players CanBeTriggeredByPlayers { get; set; }
        public bool CanBeTriggeredByAI { get; set; }
        public bool CancelAfterFirstVisit { get; set; }
    }
}