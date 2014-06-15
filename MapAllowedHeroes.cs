using System.Linq;

namespace H3Mapper
{
    public class MapAllowedHeroes
    {
        public bool[] BitMask { get; set; }
        public int[] Placeholders { get; set; }

        public override string ToString()
        {
            var enabledHeroesCount = BitMask.Count(x => x);
            return string.Format("Count: {0}, Placeholders: {1}", enabledHeroesCount, Placeholders);
        }
    }
}