using System;
using System.IO;

namespace H3Mapper.Internal
{
    public static class StreamExtensions
    {
        public static byte[] ReadBuffer(this Stream stream, int byteCount)
        {
            var bytes = new byte[byteCount];

            var readCount = stream.Read(bytes, 0, bytes.Length);

            if (readCount != byteCount)
            {
                throw new InvalidOperationException(
                    $"Unexpected amount of data. Expected {byteCount} bytes but {readCount} read.");
            }

            return bytes;
        }
    }
}