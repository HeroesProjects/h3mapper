using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using H3Mapper.Flags;
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
            var enumValue = (TEnum) Enum.ToObject(typeof(TEnum), rawValue);
            if (!Enum.IsDefined(typeof(TEnum), enumValue))
            {
                Log.Warning(
                    "Unexpected enum value {value} for enum type {enumType}", enumValue, typeof(TEnum));
            }

            return enumValue;
        }

        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct
        {
            // simplistic implementation. A valid value would be something like `Foo` or `Foo, Bar` not `0` or `15`
            return int.TryParse(value.ToString(), out _) == false;
        }
    }
}