using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class HeroMainTown
    {
        public HeroMainTown()
        {
            GenerateHero = true;
        }
        // Not specified here in RoE, must be derived from the town itself
        public Faction? Faction { get; set; }
        public MapPosition Position { get; set; }
        public bool GenerateHero { get; set; }
    }
}