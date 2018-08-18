namespace H3Mapper.MapObjects
{
    public class MessageObject : MapObject
    {
        public MessageObject(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}