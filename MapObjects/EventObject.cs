using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class EventObject : MapObject
    {
        public int GainedExperience { get; set; }
        public int ManaDifference { get; set; }
        public int MoraleDifference { get; set; }
        public int LuckDifference { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public Identifier[] Artifacts { get; set; }
        public Identifier[] Spells { get; set; }
        public MapCreature[] Creatures { get; set; }
        public Players CanBeTriggeredByPlayers { get; set; }
        public bool CanBeTriggeredByAI { get; set; }
        public bool CancelAfterFirstVisit { get; set; }
    }
}