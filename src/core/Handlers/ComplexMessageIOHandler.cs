using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using Microsoft.Extensions.Logging;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    /// <summary>
    /// Complex Message IOHandler with both simple message and stream supported
    /// </summary>
    public class ComplexMessageIOHandler : AbstractIOHandler
    {
        private static class MessageType
        {
            public static int Message = 1;
            public static int ByteStream = 2;
        }

        private readonly byte[] headerPayload = new byte[12]; // 4 bytes MessageType, 8 bytes body length
        private readonly byte[] payloadBuffer;
        private long streamReadBytesLeft;

        /// <summary>
        /// for any message larger than maxMessageSize, it is advised to use stream instead
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="maxMessageSize"></param>
        internal ComplexMessageIOHandler(ILogger<ComplexMessageIOHandler> logger, int maxMessageSize = 4096)
            : base(logger)
        {
            payloadBuffer = new byte[maxMessageSize];
        }

        public event ReadOnlySpanAction<byte, int> OnNewMessage;
        public event Action<Stream> OnStream;
        private MemoryStream memoryStream;

        public override void OnReceive(ReadOnlySpan<byte> data)
        {
            base.OnReceive(data);
            Span<byte> header = headerPayload;
            while (streamReadBytesLeft == 0 && RecvBuffer.Peek(header) == 12)
            {
                // use network endian
                var messageType = BinaryPrimitives.ReadInt32BigEndian(header);
                var payloadLength = BinaryPrimitives.ReadInt64BigEndian(header.Slice(4));
                if (messageType == MessageType.Message)
                {
                    if (payloadLength > payloadBuffer.Length)
                    {
                        throw new IndexOutOfRangeException(
                            $"Message({payloadLength}) is too big to fit into the buffer({payloadBuffer.Length})");
                    }

                    if (payloadLength + 12 <= RecvBuffer.Length)
                    {
                        RecvBuffer.Skip(12);
                        Span<byte> messageSpan = payloadBuffer;
                        messageSpan = messageSpan.Slice(0, (int) payloadLength);
                        var count = RecvBuffer.Take(messageSpan);
                        OnNewMessage?.Invoke(messageSpan, count);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (messageType == MessageType.ByteStream)
                {
                    if (memoryStream == null)
                    {
                        memoryStream = new MemoryStream(1024 * 1024 * 4);
                    }

                    memoryStream.SetLength(0);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    streamReadBytesLeft = payloadLength;
                    RecvBuffer.Skip(12);

                    DrainStreamPayload();
                }
            }

            while (streamReadBytesLeft > 0)
            {
                if (DrainStreamPayload() == 0)
                {
                    break;
                }
            }
        }

        public void SendMessage(ReadOnlySpan<byte> data)
        {
            if (data.Length > 0)
            {
                Span<byte> headerSpan = headerPayload;
                BinaryPrimitives.WriteInt32BigEndian(headerSpan, MessageType.Message);
                BinaryPrimitives.WriteInt64BigEndian(headerSpan.Slice(4), data.Length);

                SendInternal(headerSpan);
                SendInternal(data);
                Logger.LogDebug(
                    $"sending message: [{BitConverter.ToString(headerSpan.ToArray())}] {BitConverter.ToString(data.ToArray())}");
            }
        }


        private readonly byte[] streamWriteBuffer = new byte[4096];
        private Stream streamToSend;
        private long sendBytesLeft;

        public void SendStream(Stream stream)
        {
            if (stream?.Length > 0)
            {
                stream.Seek(0, SeekOrigin.Begin);
                streamToSend = stream;
                sendBytesLeft = stream.Length;
                Span<byte> headerSpan = headerPayload;
                BinaryPrimitives.WriteInt32BigEndian(headerSpan, MessageType.ByteStream);
                BinaryPrimitives.WriteInt64BigEndian(headerSpan.Slice(4), stream.Length);
                SendInternal(headerSpan);
            }
        }

        internal override void OnSent(int bytes)
        {
            base.OnSent(bytes);
            if (sendBytesLeft > 0)
            {
                Span<byte> buff = streamWriteBuffer;
                buff = buff.Slice(0, (int) Math.Min(SendBuffer.Available, sendBytesLeft));
                sendBytesLeft -= streamToSend.Read(buff);
                SendInternal(buff);
            }
        }

        private int DrainStreamPayload()
        {
            Span<byte> payloadSpan = payloadBuffer;
            payloadSpan = payloadSpan.Slice(0, Math.Min((int) streamReadBytesLeft, payloadBuffer.Length));
            var count = RecvBuffer.Take(payloadSpan);
            memoryStream.Write(payloadSpan.Slice(0, count));
            streamReadBytesLeft -= count;
            if (streamReadBytesLeft == 0)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                OnStream?.Invoke(memoryStream);
            }

            return count;
        }
    }
}