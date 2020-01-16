using System;
using Microsoft.Extensions.Logging;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public sealed class EchoIOHandler : AbstractIOHandler
    {
        private readonly byte[] buff = new byte[4096];

        internal EchoIOHandler(ILogger<EchoIOHandler> logger) : base(logger)
        {
        }

        public override void OnReceive(ReadOnlySpan<byte> data)
        {
            base.OnReceive(data);

            Span<byte> buff2 = buff;
            var count = RecvBuffer.Take(buff2);
            buff2 = buff2.Slice(0, count);

            Logger.LogDebug($"received: {BitConverter.ToString(buff2.ToArray())}");

            SendInternal(buff2);
        }
    }
}