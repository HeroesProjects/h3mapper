using System;

namespace H3Mapper
{
    [Flags]
    public enum Players : byte
    {
        None = 0,
        Red = 1 << 0,
        Blue = 1 << 1,
        Tan = 1 << 2,
        Green = 1 << 3,
        Orange = 1 << 4,
        Purple = 1 << 5,
        Teal = 1 << 6,
        Pink = 1 << 7
    }
}