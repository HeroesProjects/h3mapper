using System.Collections.Generic;

namespace H3Mapper
{
    public class MapEvent : MapObject
    {
        public int GainedExperience { get; set; }
        public int ManaDifference { get; set; }
        public int MoraleDifference { get; set; }
        public int LuckDifference { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public IDictionary<PrimarySkillType, int> PrimarySkills { get; set; }
        public SecondarySkill[] SecondarySkills { get; set; }
        public Artifact[] Artifacts { get; set; }
        public int[] Spells { get; set; }
        public MapCreature[] Creatures { get; set; }
        public Players CanBeTriggeredByPlayers { get; set; }
        public bool CanBeTriggeredByAI { get; set; }
        public bool CancelAfterFirstVisit { get; set; }
    }
}