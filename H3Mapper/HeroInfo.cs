namespace H3Mapper
{
    public class HeroInfo
    {
        public Identifier Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return string.Format("Value: {0}, Name: {1}", Id, Name);
        }
    }
}