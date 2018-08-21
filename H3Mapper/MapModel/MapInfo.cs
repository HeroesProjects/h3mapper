using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class MapInfo
    {
        public MapInfo()
        {
            AllowSpecialWeeks = true;
        }

        public MapFormat Format { get; set; }
        public MapFormatSubversion FormatSubversion { get; set; }
        public bool HasPlayers { get; set; }
        public int Size { get; set; }
        public bool HasSecondLevel { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Difficulty Difficulty { get; set; }
        public int ExperienceLevelLimit { get; set; }
        public bool AllowSpecialWeeks { get; set; }
        public MapPlayer[] Players { get; set; }
        public VictoryCondition VictoryCondition { get; set; }
        public LossCondition LossCondition { get; set; }
    }
}