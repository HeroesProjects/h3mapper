namespace H3Mapper
{
    public class MapObject
    {
        private readonly CustomObject template;

        public MapObject(CustomObject template)
        {
            this.template = template;
        }

        public MapObject()
        {
        }

        public CustomObject Template
        {
            get { return template; }
        }

        public MapPosition Position { get; set; }
        public string Message { get; set; }
        public MapCreature[] Guards { get; set; }
    }
}