using H3Mapper.Flags;
using H3Mapper.MapModel;

namespace H3Mapper.MapObjects
{
    public class HeroPlaceholderObject : MapObject
    {
        public Player Owner { get; set; }
        public int? PowerRating { get; set; }
        public Identifier Id { get; set; }
    }
}