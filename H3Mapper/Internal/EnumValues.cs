using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Serilog;

namespace H3Mapper.Internal
{
    public static class EnumValues
    {
        public static IEnumerable<TEnum> For<TEnum>() where TEnum : struct
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        }

        public static TEnum Cast<TEnum>(int rawValue) where TEnum : struct
        {
            var enumValue = (TEnum)Enum.ToObject(typeof(TEnum), rawValue);
            if (!Enum.IsDefined(typeof(TEnum), enumValue))
            {
                Log.Warning(
                    "Unexpected enum value {value} for enum type {enumType}", enumValue, typeof(TEnum));
            }

            return enumValue;
        }
    }
}