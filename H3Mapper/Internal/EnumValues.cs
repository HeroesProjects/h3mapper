using System;
using System.Collections.Generic;
using System.Linq;

namespace H3Mapper.Internal
{
    public static class EnumValues
    {
        public static IEnumerable<TEnum> For<TEnum>() where TEnum : struct
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        }
    }
}