using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace H3Mapper
{
    public class MapDeserializer
    {
        private readonly IDictionary<Type, Func<Stream, object>> deserializers =
            new Dictionary<Type, Func<Stream, object>>();

        private readonly IDictionary<Type, Func<byte[], object>> deserializers2 =
            new Dictionary<Type, Func<byte[], object>>();

        private readonly Action<string> log;
        private readonly Stream map;

        public MapDeserializer(Stream mapFile, Action<string> log)
        {
            map = mapFile;
            this.log = log;
            deserializers2.Add(typeof (BitArray), b => new BitArray(b));
        }

        public string Location
        {
            get { return map.Position.ToString("X8"); }
        }

        public T Read<T>(int byteCount)
        {
            var raw = new byte[byteCount];
            map.Read(raw, 0, raw.Length);
            return Convert<T>(raw);
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
                var value = Convert(raw, type.GetEnumUnderlyingType());
                if (Enum.IsDefined(type, value) == false)
                {
                    log("Unrecognised value for " + type.Name + ": " + value);
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

        private object ConvertUInt16(byte[] raw)
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

        private object ConvertBool(byte[] raw)
        {
            return BitConverter.ToBoolean(raw, 0);
        }

        private object ConvertInt32(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 0);
        }

        private object ConvertUInt32(byte[] raw)
        {
            return BitConverter.ToUInt32(raw, 0);
        }

        private object ConvertByte(byte[] raw)
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
                var stringLenght = Read<int>();
                if (stringLenght > 5000)
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
            if (map.CanSeek)
            {
                map.Seek(byteCount, SeekOrigin.Current);
            }
            else
            {
                var garbage = new byte[byteCount];
                map.Read(garbage, 0, garbage.Length);
            }
        }

        public void Log(string message)
        {
            log("Log at " + Location + ": " + message);
        }
    }
}