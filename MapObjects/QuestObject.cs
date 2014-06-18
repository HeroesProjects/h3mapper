using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class QuestObject : MapObject
    {
        public QuestType Type { get; set; }
        public IDictionary<PrimarySkillType, int> Skills { get; set; }
        public long Experience { get; set; }
        public Identifier[] Artifacts { get; set; }
        public MapMonster[] Creatues { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public int HeroId { get; set; }
        public Player PlayerId { get; set; }
        public long ReferencedId { get; set; }
        public int? Deadline { get; set; }
        public string FirstVisitText { get; set; }
        public string NextVisitText { get; set; }
        public string CompletedText { get; set; }
    }
}