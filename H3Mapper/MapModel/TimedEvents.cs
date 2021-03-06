using System.Collections.Generic;
using H3Mapper.Flags;

namespace H3Mapper.MapModel
{
    public class TimedEvents
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public Players Players { get; set; }
        public bool HumanAffected { get; set; }
        public bool ComputerAffected { get; set; }
        public int FirstOccurence { get; set; }
        public int RepeatEvery { get; set; }
        public bool[] NewBuildings { get; set; }
        public int[] Creatures { get; set; }
    }
}