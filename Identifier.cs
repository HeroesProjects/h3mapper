using System.Diagnostics;

namespace H3Mapper
{
    [DebuggerDisplay("{Value} {Name}")]
    public class Identifier
    {
        public int Value { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", Value, Name);
        }
    }
}