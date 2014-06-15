using System.Collections.Generic;

namespace H3Mapper
{
    public class TimedEvents
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public IDictionary<Resource, int> Resources { get; set; }
        public Player Players { get; set; }
        public bool HumanAffected { get; set; }
        public bool ComputerAffected { get; set; }
        public int FirstOccurence { get; set; }
        public int RepeatEvery { get; set; }
        public bool[] NewBuildings { get; set; }
        public int[] Creatures { get; set; }
    }
}