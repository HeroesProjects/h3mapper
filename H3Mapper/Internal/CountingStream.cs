using System;
using System.IO;

namespace H3Mapper.Internal
{
    public class CountingStream : Stream
    {
        private readonly Stream inner;

        public CountingStream(Stream inner)
        {
            this.inner = inner;
        }

        public override bool CanRead { get; } = true;

        public override bool CanSeek { get; } = false;

        public override bool CanWrite { get; } = false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get; set; }

        public override void Flush() => inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override void SetLength(long value) => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            Position += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
    }
}