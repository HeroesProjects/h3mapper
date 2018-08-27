using H3Mapper.Flags;
using H3Mapper.MapObjects;

namespace H3Mapper.MapModel
{
    public class H3Map
    {
        public Identifier[] AllowedArtifacts { get; set; }
        public MapRumor[] Rumors { get; set; }
        public MapTerrain Terrain { get; set; }
        public MapObject[] Objects { get; set; }
        public TimedEvents[] Events { get; set; }
        public Identifier[] AllowedSpells { get; set; }
        public SecondarySkillType[] AllowedSecondarySkills { get; set; }
        public MapInfo Info { get; set; }
        public MapHeroes Heroes { get; set; }
        public MapPlayer[] Players { get; set; }
        public VictoryCondition VictoryCondition { get; set; }
        public LossCondition LossCondition { get; set; }
    }
}