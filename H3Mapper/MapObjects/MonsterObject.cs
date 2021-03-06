using System.Collections.Generic;
using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class MonsterObject : GuardedObject
    {
        public long Identifier { get; set; }
        public int Count { get; set; }
        public Disposition Disposition { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public Identifier Artifact { get; set; }
        public bool AlwaysAttacts { get; set; }
        public bool KeepsSize { get; set; }
        public Identifier Type { get; set; }
    }
}