using System.Diagnostics;
using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    [DebuggerDisplay("{Slot} - {Artifact}")]
    public class HeroArtifact
    {
        public Identifier Artifact { get; set; }
        public ArtifactSlot Slot { get; set; }
    }
}