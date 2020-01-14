using H3Mapper.Flags;
using H3Mapper.MapObjects;

namespace H3Mapper.MapModel
{
    public class H3Map
    {
        public H3Map()
        {
            Heroes = new MapHeroes();
        }
        
        // Map Specification: General
        public MapInfo Info { get; set; }

        // Map Specification: Player Specs
        public MapPlayer[] Players { get; set; }

        public Identifier[] AllowedArtifacts { get; set; }
        public MapRumor[] Rumors { get; set; }
        public MapTerrain Terrain { get; set; }
        public MapObject[] Objects { get; set; }
        public TimedEvents[] Events { get; set; }
        public Identifier[] AllowedSpells { get; set; }
        public SecondarySkillType[] AllowedSecondarySkills { get; set; }
        public MapHeroes Heroes { get; }
        public VictoryCondition VictoryCondition { get; set; }
        public LossCondition LossCondition { get; set; }
    }
}