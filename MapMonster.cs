using System.Collections.Generic;

namespace H3Mapper
{
    public class MapMonster : MapObject
    {
        public long Identifier { get; set; }
        public int Count { get; set; }
        public Disposition Disposition { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public Artifact Artifact { get; set; }
        public bool AlwaysAttacts { get; set; }
        public bool KeepsSize { get; set; }
    }
}