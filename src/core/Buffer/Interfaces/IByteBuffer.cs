using System;

namespace RoyLab.ThunderSocket.Core.Buffer.Interfaces
{
    public interface IByteBuffer
    {
        int Length { get; }

        /// <summary>
        /// number of bytes available to write
        /// </summary>
        int Available { get; }

        /// <summary>
        /// append data into the byte buffer
        /// </summary>
        /// <param name="data">the ReadOnlySpan&lt;byte&gt; containing data to be added</param>
        void Append(ReadOnlySpan<byte> data);

        int Peek(Span<byte> data);

        /// <summary>
        /// reset the byte buffer for reuse
        /// </summary>
        void Reset();

        void Skip(int length);

        /// <summary>
        /// take out data from the byte buffer the number of bytes read is the minimum of
        /// - free bytes in data
        /// - number of bytes available in the byte buffer
        /// </summary>
        /// <param name="data"></param>
        /// <returns>the number of bytes read</returns>
        int Take(Span<byte> data);
    }
}