using System.Collections;
using System.Collections.Generic;

namespace H3Mapper.MapModel
{
    public class MapHeroes:IEnumerable<MapHero>
    {
        private readonly IDictionary<Identifier, MapHero> heroes =
            new Dictionary<Identifier, MapHero>(Identifier.ValueComparer);

        public void AddHero(Identifier heroId)
        {
            heroes.Add(heroId, new MapHero {Id = heroId});
        }

        public MapHero GetHero(Identifier heroId)
        {
            if (heroes.TryGetValue(heroId, out var hero) == false)
            {
                hero = new MapHero{Id = heroId};
                heroes.Add(heroId, hero);
            }
            return hero;
        }

        public IEnumerator<MapHero> GetEnumerator()
        {
            return heroes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}