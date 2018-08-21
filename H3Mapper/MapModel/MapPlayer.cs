using System.Collections.Generic;
using System.Linq;
using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class MapPlayer
    {
        private readonly IList<HeroInfo> heroInfos = new List<HeroInfo>();

        public MapPlayer(Player player)
        {
            Player = player;
            GenerateHeroAtMainTown = true;
        }

        public IEnumerable<HeroInfo> Heroes => heroInfos.AsEnumerable();

        public bool CanHumanPlay { get; set; }
        public bool CanAIPlay { get; set; }

        public bool CanPlay => CanAIPlay || CanHumanPlay;

        public AITactic AITactic { get; set; }
        public bool AllowedAlignmentsCustomised { get; set; }
        public Factions AllowedFactions { get; set; }
        public bool IsFactionRandom { get; set; }
        public bool HasHomeTown { get; set; }
        public bool GenerateHeroAtMainTown { get; set; }
        public Faction? MainTownType { get; set; }
        public MapPosition HomeTownPosition { get; set; }
        public bool HasRandomHero { get; set; }
        public Identifier MainCustomHero { get; set; }
        public int? MainCustomHeroPortraitId { get; set; }
        public string MainCustomHeroName { get; set; }
        public int HeroPlaceholderCount { get; set; }
        public int? TeamId { get; set; }
        public Player Player { get; }

        public void AddHero(HeroInfo heroInfo)
        {
            heroInfos.Add(heroInfo);
        }
    }
}