using System;

namespace H3Mapper
{
    public class MapPosition : IEquatable<MapPosition>
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
                    X = byte.MaxValue,
                    Y = byte.MaxValue,
                    Z = byte.MaxValue
                };
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", X, Y, Z);
        }

        public bool Equals(MapPosition other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MapPosition) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode*397) ^ Y;
                hashCode = (hashCode*397) ^ Z;
                return hashCode;
            }
        }

        public static bool operator ==(MapPosition left, MapPosition right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MapPosition left, MapPosition right)
        {
            return !Equals(left, right);
        }
    }
}