using System.Text;

namespace H3Mapper
{
    public class Position
    {
        public bool[,] Positions { get; set; }

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