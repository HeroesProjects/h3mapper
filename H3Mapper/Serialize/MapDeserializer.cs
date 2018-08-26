using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Serilog;

namespace H3Mapper.Serialize
{
    public class MapDeserializer
    {
        private readonly Stream map;
        private readonly EncodingDetector encodingDetector = new EncodingDetector();

        public MapDeserializer(Stream mapFile)
        {
            map = mapFile;
        }

        public string LocationHex => Location.ToString("X8");

        public long Location => map.Position;

        private byte[] ReadBytes(int byteCount)
        {
            var buffer = new byte[byteCount];
            var readCount = map.Read(buffer, 0, buffer.Length);
            if (readCount != byteCount)
            {
                throw new InvalidOperationException(
                    $"Unexpected amount of data. Expected {byteCount} bytes but {readCount} read.");
            }

            return buffer;
        }

        private object Convert(byte[] raw, Type type)
        {
            if (type == typeof(int))
            {
                return ConvertInt32(raw);
            }

            if (type == typeof(byte))
            {
                return ConvertByte(raw);
            }

            if (type == typeof(sbyte))
            {
                return (sbyte) ConvertByte(raw);
            }

            if (type == typeof(ushort))
            {
                return ConvertUInt16(raw);
            }

            throw new NotSupportedException();
        }

        private static bool IsEnumDefined(Type type, object value)
        {
            return Enum.ToObject(type, value).ToString() != value.ToString();
        }

        private short ConvertInt16(byte[] raw)
        {
            return BitConverter.ToInt16(raw, 0);
        }

        private ushort ConvertUInt16(byte[] raw)
        {
            return BitConverter.ToUInt16(raw, 0);
        }

        private string ConvertString(byte[] raw)
        {
            return encodingDetector.GuessEncoding(raw).GetString(raw);
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

        public int Read1ByteNumber(byte minValue = 0, byte maxValue = byte.MaxValue, bool allowEmpty = false)
        {
            var location = Location;
            var bytes = ReadBytes(1);
            var value = ConvertByte(bytes);
            if (allowEmpty == false || value != byte.MaxValue)
            {
                EnsureRange(minValue, maxValue, value, location);
            }

            return value;
        }

        public int Read1ByteSignedNumber(sbyte minValue = sbyte.MinValue, sbyte maxValue = sbyte.MaxValue)
        {
            var location = Location;
            var bytes = ReadBytes(1);
            var value = (sbyte) ConvertByte(bytes);
            EnsureRange(minValue, maxValue, value, location);
            return value;
        }

        public int Read2ByteNumber(int minValue = 0, int maxValue = ushort.MaxValue)
        {
            var location = Location;
            var bytes = ReadBytes(2);
            var value = ConvertUInt16(bytes);
            EnsureRange(minValue, maxValue, value, location);
            return value;
        }

        public int Read2ByteNumberSigned(short minValue = short.MinValue, short maxValue = short.MaxValue)
        {
            var location = Location;
            var bytes = ReadBytes(2);
            var value = ConvertInt16(bytes);
            EnsureRange(minValue, maxValue, value, location);
            return value;
        }

        public int Read4ByteNumber(int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            var location = Location;
            var bytes = ReadBytes(4);
            var value = ConvertInt32(bytes);
            EnsureRange(minValue, maxValue, value, location);
            return value;
        }

        public long Read4ByteNumberLong(long minValue = 0L, long maxValue = uint.MaxValue)
        {
            var location = Location;
            var bytes = ReadBytes(4);
            var value = ConvertUInt32(bytes);
            EnsureRange(minValue, maxValue, value, location);
            return value;
        }

        public bool[] ReadBitmaskBits(int bitCount)
        {
            var byteCount = (int) Math.Ceiling((bitCount / (decimal) 8));
            return ReadBitmask(byteCount, bitCount);
        }

        public bool[] ReadBitmask(int byteCount)
        {
            return ReadBitmask(byteCount, byteCount * 8);
        }

        public bool ReadBool()
        {
            var location = Location;
            var bytes = ReadBytes(1);
            var value = bytes[0];
            if (value == 0)
            {
                return false;
            }

            if (value != 1)
            {
                Log.Warning("Boolean at {location:X8} has unexpected value of {value:X8}", location, value);
            }

            return true;
        }

        public string ReadString(int maxLength)
        {
            var location = Location;
            var stringLength = Read4ByteNumber(minValue: 0);
            if (stringLength > maxLength)
            {
                Log.Warning(
                    "String at {location:X8} has length of {length} which is above the expected limit of {limit}",
                    location, stringLength, maxLength);
            }

            if (stringLength < 0)
            {
                throw new ArgumentException(
                    $"String at {location:x8} has negative length of {stringLength}." +
                    $" It\'s either a bug or the map file is invalid.");
            }

            var bytes = ReadBytes(stringLength);
            return ConvertString(bytes);
        }

        private bool[] ReadBitmask(int byteCount, int bitCount)
        {
            var bytes = ReadBytes(byteCount);
            var bitArray = new BitArray(bytes);
            return bitArray.OfType<bool>().Take(bitCount).ToArray();
        }

        private void EnsureRange(long min, long max, long value, long location)
        {
            if (value < min || value > max)
            {
                Log.Warning("Value {value} at {location:X8} is out of range ({min} - {max})", value, location, min,
                    max);
            }
        }

        public T ReadEnum<T>(int? bytesCount = null) where T : struct
        {
            var type = typeof(T);
            Debug.Assert(type.IsEnum);
            var underlyingType = type.GetEnumUnderlyingType();
            var location = Location;

            var size = SizeOf(underlyingType);
            Debug.Assert(size >= bytesCount.GetValueOrDefault(size));
            var bytes = ReadBytes(bytesCount.GetValueOrDefault(size));
            if (bytes.Length < size)
            {
                Array.Resize(ref bytes, size);
            }

            var rawValue = Convert(bytes, underlyingType);

            if (IsEnumDefined(type, rawValue) == false)
            {
                Log.Information("Unrecognised value for {type}: {value:X8} at {location:X8}", type, rawValue, location);
            }

            return (T) rawValue;
        }

        private int SizeOf(Type type)
        {
            if (type == typeof(bool))
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
            var location = Location;
            var garbage = ReadBytes(byteCount);
            if (garbage.Any(b => b != 0))
            {
                Log.Warning(
                    "Skipped {count} bytes at {location:X8}, but not all of them are empty. Bytes: {bytes}",
                    byteCount,
                    location,
                    BitConverter.ToString(garbage));
            }
        }

        public void EnsureEof(int checkByteCount)
        {
            var location = Location;
            var buffer = new byte[checkByteCount];
            var readCount = map.Read(buffer, 0, buffer.Length);
            if (readCount != 0)
            {
                Log.Warning(
                    "Unexpected data at the end of the file at {location}. Read {readCount} bytes: {bytes}",
                    location.ToString("x8"),
                    readCount,
                    BitConverter.ToString(buffer));
            }
        }

        public void Ignore(int byteCount)
        {
            ReadBytes(byteCount);
        }
    }
}