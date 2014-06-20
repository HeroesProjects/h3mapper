using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;

namespace H3Mapper
{
    public class MapDeserializer
    {
        private readonly Stream map;

        public MapDeserializer(Stream mapFile)
        {
            map = mapFile;
        }

        public string LocationHex
        {
            get { return map.Position.ToString("X8"); }
        }

        public long Location
        {
            get { return map.Position; }
        }

        private byte[] ReadBytes(int byteCount)
        {
            var raw = new byte[byteCount];
            map.Read(raw, 0, raw.Length);
            return raw;
        }

        private object Convert(byte[] raw, Type type)
        {
            if (type == typeof (int))
            {
                return ConvertInt32(raw);
            }
            if (type == typeof (byte))
            {
                return ConvertByte(raw);
            }
            if (type == typeof (ushort))
            {
                return ConvertUInt16(raw);
            }
            throw new NotSupportedException();
        }

        private static bool IsEnumDefined(Type type, object value)
        {
            return Enum.ToObject(type, value).ToString() != value.ToString();
        }

        private ushort ConvertUInt16(byte[] raw)
        {
            return BitConverter.ToUInt16(raw, 0);
        }

        private string ConvertUtf8String(byte[] raw)
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

        public bool[] ReadBitmaskBits(int bitCount)
        {
            var byteCount = (int) Math.Ceiling((bitCount/(decimal) 8));
            return ReadBitmask(byteCount, bitCount);
        }

        public bool[] ReadBitmask(int byteCount)
        {
            return ReadBitmask(byteCount, byteCount*8);
        }

        private bool[] ReadBitmask(int byteCount, int bitCount)
        {
            var bytes = ReadBytes(byteCount);
            var bitArray = new BitArray(bytes);
            return bitArray.OfType<bool>().Take(bitCount).ToArray();
        }

        public bool ReadBool()
        {
            var bytes = ReadBytes(1);
            return ConvertBool(bytes);
        }

        public string ReadString()
        {
            var stringLenght = Read4ByteNumber();
            if (stringLenght > 50000)
            {
                throw new ArgumentOutOfRangeException("",
                    string.Format(
                        "The string length of {0} looks a bit large. Perhaps something wrong with the file?",
                        stringLenght));
            }
            var bytes = ReadBytes(stringLenght);
            return ConvertUtf8String(bytes);
        }

        public T ReadEnum<T>() where T : struct
        {
            var type = typeof (T);
            Debug.Assert(type.IsEnum);
            var underlyingType = type.GetEnumUnderlyingType();
            var location = Location;

            var bytes = ReadBytes(SizeOf(underlyingType));
            var rawValue = Convert(bytes, underlyingType);

            if (IsEnumDefined(type, rawValue) == false)
            {
                Log.Debug("Unrecognised value for {type}: {value} at {location:X8}", type, rawValue, location);
            }
            return (T) rawValue;
        }

        private int SizeOf(Type type)
        {
            if (type == typeof (bool))
            {
                return 1;
            }
            if (type.IsPrimitive)
            {
                return Marshal.SizeOf(type);
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