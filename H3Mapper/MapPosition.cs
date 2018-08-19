namespace H3Mapper
{
    public class MapPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public bool IsEmpty =>
            X == byte.MaxValue &&
            Y == byte.MaxValue &&
            Z == byte.MaxValue;

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", X, Y, Z);
        }
    }
}