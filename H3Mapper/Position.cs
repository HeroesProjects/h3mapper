using System.Text;

namespace H3Mapper
{
    public class Position
    {
        public Position(bool[,] positions)
        {
            Positions = positions;
        }

        protected bool Equals(Position other)
        {
            return Positions.ToString().Equals(other.Positions.ToString());
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Position) obj);
        }

        public override int GetHashCode()
        {
            return Positions.ToString().GetHashCode();
        }

        public bool[,] Positions { get; }

        public override string ToString()
        {
            var result = new StringBuilder();
            var positions = Positions;
            for (var i = 0; i <= positions.GetUpperBound(0); i++)
            {
                for (var j = 0; j <= positions.GetUpperBound(1); j++)
                {
                    result.Append(positions[i, j] ? "#" : "_");
                }

                result.AppendLine();
            }

            return result.ToString();
        }
    }
}