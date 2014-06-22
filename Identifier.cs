using System.Collections.Generic;
using System.Diagnostics;

namespace H3Mapper
{
    [DebuggerDisplay("{Value} {Name}")]
    public class Identifier
    {
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

        private static readonly IEqualityComparer<Identifier> ValueComparerInstance = new ValueEqualityComparer();

        public static IEqualityComparer<Identifier> ValueComparer
        {
            get { return ValueComparerInstance; }
        }

        public int Value { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Value, Name);
        }
    }
}