namespace H3Mapper.MapObjects
{
    public class GuardedObject : MapObject
    {
        public string Message { get; set; }
        public MapMonster[] Guards { get; set; }
    }
}