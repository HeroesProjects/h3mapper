using System.Collections.Generic;
using System.Diagnostics;

namespace H3Mapper.MapModel
{
    [DebuggerDisplay("{Value} {Name}")]
    public class Identifier
    {
        public int Value { get; set; }
        public string Name { get; set; }

        public static IEqualityComparer<Identifier> ValueComparer { get; } = new ValueEqualityComparer();
        
        private sealed class ValueEqualityComparer : IEqualityComparer<Identifier>
        {
            public bool Equals(Identifier x, Identifier y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Value == y.Value;
            }

            public int GetHashCode(Identifier obj)
            {
                return obj.Value;
            }
        }
    }
}