using System.Diagnostics;

namespace H3Mapper
{
    [DebuggerDisplay("{Value} {Name}")]
    public class Identifier
    {
        public int Value { get; set; }
        public string Name { get; set; }
    }
}