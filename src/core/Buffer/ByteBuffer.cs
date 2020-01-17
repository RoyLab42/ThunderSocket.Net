using System;
using RoyLab.ThunderSocket.Core.Buffer.Interfaces;

namespace RoyLab.ThunderSocket.Core.Buffer
{
    /// <summary>
    /// byte buffer which is not thread safe
    /// </summary>
    public sealed class ByteBuffer : IByteBuffer
    {
        private readonly byte[] buffer;
        private readonly int bufferSize;
        private int begin;
        private int end;

        public ByteBuffer(int bufferSize)
        {
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
            Reset();
        }

        public int Length { get; private set; }

        public int Available => bufferSize - Length;

        /// <summary>
        /// copy input data into the buffer
        /// </summary>
        /// <param name="data"></param>
        public void Append(ReadOnlySpan<byte> data)
        {
            if (Length + data.Length > bufferSize)
            {
                throw new Exception("buffer over flow, please make it bigger");
            }

            Span<byte> dst = buffer;
            if (end + data.Length <= bufferSize)
            {
                dst = dst.Slice(end, data.Length);
                data.CopyTo(dst);
            }
            else
            {
                var src1 = data.Slice(0, bufferSize - end);
                var src2 = data.Slice(src1.Length, data.Length - src1.Length);
                src1.CopyTo(dst.Slice(end, bufferSize - end));
                src2.CopyTo(dst);
            }

            end = (end + data.Length) % bufferSize;
            Length += data.Length;
        }

        public int Peek(Span<byte> data)
        {
            var count = Math.Min(Length, data.Length);
            Span<byte> src = buffer;
            if (begin + count <= bufferSize)
            {
                src.Slice(begin, count).CopyTo(data);
            }
            else
            {
                var srcPart1 = src.Slice(begin, bufferSize - begin);
                var srcPart2 = src.Slice(0, count - srcPart1.Length);
                srcPart1.CopyTo(data);
                srcPart2.CopyTo(data.Slice(srcPart1.Length, srcPart2.Length));
            }

            return count;
        }

        /// <summary>
        /// reset the byte buffer for reuse
        /// </summary>
        public void Reset()
        {
            begin = 0;
            end = 0;
            Length = 0;
        }

        public void Skip(int count)
        {
            begin = (begin + count) % bufferSize;
            Length -= count;
        }

        public int Take(Span<byte> data)
        {
            var count = Peek(data);
            Skip(count);
            return count;
        }
    }
}