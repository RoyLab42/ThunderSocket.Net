using System;
using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Buffer;
using RoyLab.ThunderSocket.Core.Buffer.Interfaces;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;
using RoyLab.ThunderSocket.Core.Mux;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public abstract class AbstractIOHandler : IOHandler
    {
        private const int ReceiveBufferSize = AbstractTcpMux.ReceiveBufferSize * 2;
        private const int SendBufferSize = AbstractTcpMux.SendBufferSize;

        protected readonly ILogger Logger;
        protected readonly IByteBuffer RecvBuffer;
        protected readonly IByteBuffer SendBuffer;

        private bool isSending;

        protected AbstractIOHandler(ILogger logger)
        {
            Logger = logger;
            RecvBuffer = new ByteBuffer(ReceiveBufferSize);
            SendBuffer = new ByteBuffer(SendBufferSize);
        }

        internal virtual bool IsWritable { get; set; }

        /// <summary>
        /// The underlying function of how data was sent out.
        /// Send out data from the send buffer, and return the number of bytes successfully sent.
        /// </summary>
        internal Func<IByteBuffer, int> SendImplement { get; set; }

        public virtual void OnReceive(ReadOnlySpan<byte> data)
        {
            RecvBuffer.Append(data);
        }

        public void Reset()
        {
            RecvBuffer.Reset();
            SendBuffer.Reset();
        }

        internal virtual void OnSent(int bytes)
        {
            isSending = false;
            if (SendBuffer.Length > 0)
            {
                isSending = true;
                SendImplement(SendBuffer);
            }
        }

        protected void SendInternal(ReadOnlySpan<byte> data)
        {
            if (IsWritable)
            {
                SendBuffer.Append(data);
                if (!isSending)
                {
                    isSending = true;
                    SendImplement(SendBuffer);
                }
            }
            else
            {
                Logger.LogError("send buffer is not writable yet!");
            }
        }
    }
}