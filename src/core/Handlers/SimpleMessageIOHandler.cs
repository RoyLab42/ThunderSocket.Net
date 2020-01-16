using System;
using System.Buffers;
using Microsoft.Extensions.Logging;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public class SimpleMessageIOHandler : AbstractIOHandler
    {
        private readonly byte[] headerPayload = new byte[4];
        private readonly byte[] payload;

        internal SimpleMessageIOHandler(ILogger<SimpleMessageIOHandler> logger, int maxMessageSize = 32768)
            : base(logger)
        {
            payload = new byte[maxMessageSize];
        }

        public event ReadOnlySpanAction<byte, int> OnNewMessage;

        public override void OnReceive(ReadOnlySpan<byte> data)
        {
            base.OnReceive(data);
            Span<byte> headerSpan = headerPayload;
            while (RecvBuffer.Peek(headerSpan) == 4)
            {
                if (BitConverter.IsLittleEndian)
                {
                    headerSpan.Reverse();
                }

                var payloadLength = BitConverter.ToInt32(headerSpan);
                if (payloadLength > payload.Length)
                {
                    throw new IndexOutOfRangeException("not enough buffer for message");
                }

                if (payloadLength + 4 <= RecvBuffer.Length)
                {
                    RecvBuffer.Skip(4);
                    Span<byte> messageSpan = payload;
                    messageSpan = messageSpan.Slice(0, payloadLength);
                    var count = RecvBuffer.Take(messageSpan);
                    OnNewMessage?.Invoke(messageSpan, count);
                }
                else
                {
                    break;
                }
            }
        }

        public void Send(ReadOnlySpan<byte> data)
        {
            if (data.Length > 0)
            {
                Span<byte> headerSpan = BitConverter.GetBytes(data.Length);
                if (BitConverter.IsLittleEndian)
                {
                    headerSpan.Reverse();
                }

                SendInternal(headerSpan);
                SendInternal(data);
                Logger.LogDebug(
                    $"sending message: [{BitConverter.ToString(headerSpan.ToArray())}] {BitConverter.ToString(data.ToArray())}");
            }
        }
    }
}