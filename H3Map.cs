using H3Mapper.Flags;
using H3Mapper.MapObjects;

namespace H3Mapper
{
    public class H3Map
    {
        public H3Map()
        {
            AllowSpecialWeeks = true;
        }

        public MapFormat Format { get; set; }
        public bool HasPlayers { get; set; }
        public int Size { get; set; }
        public bool HasSecondLevel { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Difficulty Difficulty { get; set; }
        public int ExperienceLevelLimit { get; set; }
        public MapPlayer[] Players { get; set; }
        public VictoryCondition VictoryCondition { get; set; }
        public LossCondition LossCondition { get; set; }
        public int TeamCount { get; set; }
        public MapAllowedHeroes AllowedHeroes { get; set; }
        public DisposedHero[] DisposedHeroes { get; set; }
        public Identifier[] AllowedArtifacts { get; set; }
        public MapRumor[] Rumors { get; set; }
        public MapHeroDefinition[] PrefedinedHeroes { get; set; }
        public MapTerrain Terrain { get; set; }
        public MapObject[] Objects { get; set; }
        public TimedEvents[] Events { get; set; }
        public bool AllowSpecialWeeks { get; set; }
        public Identifier[] AllowedSpells { get; set; }
        public SecondarySkillType[] AllowedSecondarySkills { get; set; }
    }
}