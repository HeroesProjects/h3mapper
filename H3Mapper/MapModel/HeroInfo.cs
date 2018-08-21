using System.Diagnostics;

namespace H3Mapper.MapModel
{
    [DebuggerDisplay("{Id.Value} ({Id.Name}) - {Name}")]
    public class HeroInfo
    {
        public Identifier Id { get; set; }
        public string Name { get; set; }
    }
}