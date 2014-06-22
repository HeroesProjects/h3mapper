using System.Collections.Generic;

namespace H3Mapper
{
    public class MapHeroes
    {
        private readonly IDictionary<Identifier, MapHero> heroes =
            new Dictionary<Identifier, MapHero>(Identifier.ValueComparer);

        public void AddHero(Identifier heroId)
        {
            heroes.Add(heroId, new MapHero {Id = heroId});
        }

        public MapHero GetHero(Identifier heroId)
        {
            MapHero hero;
            if (heroes.TryGetValue(heroId, out hero) == false)
            {
                hero = new MapHero{Id = heroId};
                heroes.Add(heroId, hero);
            }
            return hero;
        }
    }
}