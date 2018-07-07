using System.Collections.Generic;
using System.Linq;
using H3Mapper.Flags;

namespace H3Mapper
{
    public class MapPlayer
    {
        private readonly IList<HeroInfo> heroInfos = new List<HeroInfo>();

        public MapPlayer()
        {
            GenerateHeroAtMainTown = true;
        }

        public IEnumerable<HeroInfo> Heroes
        {
            get { return heroInfos.AsEnumerable(); }
        }

        public bool CanHumanPlay { get; set; }
        public bool CanAIPlay { get; set; }

        public bool Disabled
        {
            get { return CanAIPlay == false && CanHumanPlay == false; }
        }

        public AITactic AITactic { get; set; }
        public int P7 { get; set; }
        public Factions AllowedFactions { get; set; }
        public bool IsFactionRandom { get; set; }
        public bool HasHomeTown { get; set; }
        public bool GenerateHeroAtMainTown { get; set; }
        public bool GenerateHero { get; set; }
        public MapPosition HomeTownPosition { get; set; }
        public bool HasRandomHero { get; set; }
        public Identifier MainCustomHero { get; set; }
        public int? MainCustomHeroPortraitId { get; set; }
        public string MainCustomHeroName { get; set; }
        public int PowerPlaceholders { get; set; }
        public int? TeamId { get; set; }

        public void AddHero(HeroInfo heroInfo)
        {
            heroInfos.Add(heroInfo);
        }
    }
}