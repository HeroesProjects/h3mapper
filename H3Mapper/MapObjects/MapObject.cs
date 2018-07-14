namespace H3Mapper.MapObjects
{
    public class MapObject
    {
        public MapObjectTemplate Template { get; set; }

        public MapPosition Position { get; set; }
        public string Message { get; set; }
        public MapMonster[] Guards { get; set; }
    }
}