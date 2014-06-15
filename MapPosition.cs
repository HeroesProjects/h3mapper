namespace H3Mapper
{
    public class MapPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public static MapPosition Empty
        {
            get
            {
                return new MapPosition
                {
                    X = -1,
                    Y = -1,
                    Z = -1
                };
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", X, Y, Z);
        }
    }
}