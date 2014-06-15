using System;
using System.IO;

namespace H3Mapper
{
    public class CountingStream : Stream
    {
        private readonly Stream inner;
        private int location;

        public CountingStream(Stream inner)
        {
            this.inner = inner;
        }

        public int Location
        {
            get { return location; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { return location; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
            inner.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            location += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}