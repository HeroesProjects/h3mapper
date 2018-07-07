namespace H3Mapper.MapObjects
{
    public class MapObject
    {
        public MapObject(MapObjectTemplate template)
        {
            Template = template;
        }

        public MapObject()
        {
        }

        public MapObjectTemplate Template { get; }

        public MapPosition Position { get; set; }
        public string Message { get; set; }
        public MapMonster[] Guards { get; set; }
    }
}