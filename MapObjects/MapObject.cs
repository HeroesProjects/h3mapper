namespace H3Mapper.MapObjects
{
    public class MapObject
    {
        private readonly MapObjectTemplate template;

        public MapObject(MapObjectTemplate template)
        {
            this.template = template;
        }

        public MapObject()
        {
        }

        public MapObjectTemplate Template
        {
            get { return template; }
        }

        public MapPosition Position { get; set; }
        public string Message { get; set; }
        public MapMonster[] Guards { get; set; }
    }
}