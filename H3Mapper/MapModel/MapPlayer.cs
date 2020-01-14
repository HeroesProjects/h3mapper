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
        }

        // Playability: Human
        public bool CanHumanPlay { get; set; }
        // Playability: Computer
        public bool CanAIPlay { get; set; }
        // Playability: Behavior
        public AITactic AITactic { get; set; }
        
        // Allowed alignments: Customize
        public bool AllowedFactionsCustomised { get; set; }
        // Allowed alignments
        public Factions AllowedFactions { get; set; }
        
        // Generate hero at:/Has main town + Generate hero at main town
        public HeroMainTown MainTown { get; set; }
        
        // Implicit properties:
        // Can be true is player has at least one random town
        public bool IsFactionRandom { get; set; }
        // true is player has at least one random hero
        public bool HasRandomHeroes { get; set; }
        // how many HeroPlaceholders this player has
        public int HeroPlaceholderCount { get; set; }
        
        public HeroInfo MainHero { get; set; }
        public int? TeamId { get; set; }
        
        // Derived properties
        public Player Player { get; }
        public bool CanPlay => CanAIPlay || CanHumanPlay;
        public IEnumerable<HeroInfo> Heroes => heroInfos.AsEnumerable();
        

        public void AddHero(HeroInfo heroInfo)
        {
            heroInfos.Add(heroInfo);
        }
    }
}