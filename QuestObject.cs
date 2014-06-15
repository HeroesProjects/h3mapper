using System.Collections.Generic;

namespace H3Mapper
{
    public class QuestObject : MapObject
    {
        public QuestType Type { get; set; }
        public IDictionary<PrimarySkillType, int> Skills { get; set; }
        public long Experience { get; set; }
        public MapPosition Location { get; set; }
        public int[] Artifacts { get; set; }
        public MapCreature[] Creatues { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public int HeroId { get; set; }
        public Player PlayerId { get; set; }
    }
}