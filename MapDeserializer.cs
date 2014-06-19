using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace H3Mapper
{
    public class MapDeserializer
    {
        private readonly IDictionary<Type, Func<Stream, object>> deserializers =
            new Dictionary<Type, Func<Stream, object>>();

        private readonly IDictionary<Type, Func<byte[], object>> deserializers2 =
            new Dictionary<Type, Func<byte[], object>>();

        private readonly Stream map;

        public MapDeserializer(Stream mapFile)
        {
            map = mapFile;
            deserializers2.Add(typeof (BitArray), b => new BitArray(b));
        }

        public string LocationHex
        {
            get { return map.Position.ToString("X8"); }
        }

        public long Location
        {
            get { return map.Position; }
        }

        public T Read<T>(int byteCount)
        {
            var raw = ReadBytes(byteCount);
            return Convert<T>(raw);
        }

        private byte[] ReadBytes(int byteCount)
        {
            var raw = new byte[byteCount];
            map.Read(raw, 0, raw.Length);
            return raw;
        }

        private T Convert<T>(byte[] raw)
        {
            return (T) Convert(raw, typeof (T));
        }

        private object Convert(byte[] raw, Type type)
        {
            Func<byte[], object> deserializer;
            if (deserializers2.TryGetValue(type, out deserializer))
            {
                return deserializer(raw);
            }
            if (type.IsEnum)
            {
                var location = Location;
                var value = Convert(raw, type.GetEnumUnderlyingType());
                var enumValue = Enum.ToObject(type, value);
                if (enumValue.ToString() == value.ToString())
                {
                    Log.Debug("Unrecognised value for {type}: {value} at {location:X8}", type, value, location);
                }
                return value;
            }
            if (type == typeof (bool))
            {
                return ConvertBool(raw);
            }
            if (type == typeof (int))
            {
                return ConvertInt32(raw);
            }
            if (type == typeof (uint))
            {
                return ConvertUInt32(raw);
            }
            if (type == typeof (string))
            {
                return ConvertToString(raw);
            }
            if (type == typeof (byte))
            {
                return ConvertByte(raw);
            }
            if (type == typeof (short))
            {
                return ConvertInt16(raw);
            }
            if (type == typeof (ushort))
            {
                return ConvertUInt16(raw);
            }
            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                return Convert(raw, type.GetGenericArguments()[0]);
            }
            throw new NotSupportedException();
        }

        private ushort ConvertUInt16(byte[] raw)
        {
            return BitConverter.ToUInt16(raw, 0);
        }

        private object ConvertInt16(byte[] raw)
        {
            return BitConverter.ToInt16(raw, 0);
        }

        private object ConvertToString(byte[] raw)
        {
            return Encoding.UTF8.GetString(raw);
        }

        private bool ConvertBool(byte[] raw)
        {
            return BitConverter.ToBoolean(raw, 0);
        }

        private int ConvertInt32(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 0);
        }

        private uint ConvertUInt32(byte[] raw)
        {
            return BitConverter.ToUInt32(raw, 0);
        }

        private byte ConvertByte(byte[] raw)
        {
            return raw[0];
        }

        public T? ReadNullable<T>(T nullValue) where T : struct
        {
            var value = Read<T>();
            if (Equals(value, nullValue))
            {
                return null;
            }
            return value;
        }

        public int Read1ByteNumber()
        {
            var bytes = ReadBytes(1);
            return ConvertByte(bytes);
        }

        public int Read2ByteNumber()
        {
            var bytes = ReadBytes(2);
            return ConvertUInt16(bytes);
        }

        public int Read4ByteNumber()
        {
            var location = Location;
            var bytes = ReadBytes(4);
            var number = ConvertInt32(bytes);
            if (0 > number)
            {
                Log.Warning(
                    "Number at {location:X8} is negative ({value}). Probably should have used Read4ByteNumberLong() instead",
                    location,
                    number);
            }
            return number;
        }

        public long Read4ByteNumberLong()
        {
            var bytes = ReadBytes(4);
            return ConvertUInt32(bytes);
        }

        public bool ReadBool()
        {
            var bytes = ReadBytes(1);
            return ConvertBool(bytes);
        }

        public string ReadString()
        {
            return Read<string>();
        }

        public T Read<T>()
        {
            Func<Stream, object> deserializer;
            if (deserializers.TryGetValue(typeof (T), out deserializer))
            {
                return (T) deserializer(map);
            }
            return Read<T>(SizeOf(typeof (T)));
        }

        private int SizeOf(Type type)
        {
            if (type.IsEnum)
            {
                return SizeOf(type.GetEnumUnderlyingType());
            }
            if (type == typeof (bool))
            {
                return 1;
            }
            if (type.IsPrimitive)
            {
                return Marshal.SizeOf(type);
            }
            if (type == typeof (string))
            {
                var stringLenght = Read4ByteNumber();
                if (stringLenght > 50000)
                {
                    throw new ArgumentOutOfRangeException("",
                        string.Format(
                            "The string length of {0} looks a bit large. Perhaps something wrong with the file?",
                            stringLenght));
                }
                return stringLenght;
            }
            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                return SizeOf(type.GetGenericArguments()[0]);
            }
            throw new NotSupportedException();
        }

        public void Skip(int byteCount)
        {
            var garbage = new byte[byteCount];
            var location = Location;
            map.Read(garbage, 0, garbage.Length);
            if (!garbage.All(b => b == 0))
            {
                Log.Debug(
                    "Skipped {count} bytes at {location:X8}, but not all of them are empty. Bytes: {bytes}",
                    byteCount,
                    location,
                    BitConverter.ToString(garbage));
            }
        }
    }
}