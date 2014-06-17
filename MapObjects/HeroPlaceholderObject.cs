using H3Mapper.Flags;

namespace H3Mapper.MapObjects
{
    public class HeroPlaceholderObject : MapObject
    {
        public Player Owner { get; set; }
        public int PowerRating { get; set; }
        public int Id { get; set; }
    }
}