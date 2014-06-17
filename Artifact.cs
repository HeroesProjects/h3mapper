using System.Diagnostics;

namespace H3Mapper
{
    [DebuggerDisplay("{ArtifactId} {ArtifactName}")]
    public class Artifact
    {
        public int ArtifactId { get; set; }
        public string ArtifactName { get; set; }
    }
}