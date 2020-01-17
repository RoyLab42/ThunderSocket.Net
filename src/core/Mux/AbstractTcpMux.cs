using System;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;
using RoyLab.ThunderSocket.Core.Mux.Interfaces;

namespace RoyLab.ThunderSocket.Core.Mux
{
    public abstract class AbstractTcpMux : IMux
    {
        public const int ReceiveBufferSize = 4096;
        public const int SendBufferSize = 4096;
        public abstract void Start();
        public abstract void Stop();
    }

    public abstract class AbstractTcpMux<T> : AbstractTcpMux where T : AbstractIOHandler
    {
        private readonly IOHandlerFactory<T> handlerFactory;
        protected readonly ILogger Logger;
        private readonly byte[] receiveBuffer = new byte[ReceiveBufferSize];

        protected AbstractTcpMux(ILogger logger, IOHandlerFactory<T> handlerFactory)
        {
            Logger = logger;
            this.handlerFactory = handlerFactory;
        }

        protected void OnConnected(Socket socket)
        {
            var handler = handlerFactory.CreateNewHandler();
            // send callback
            var sendAsyncEventArgs = new SocketAsyncEventArgs {UserToken = handler};
            sendAsyncEventArgs.SetBuffer(new byte[SendBufferSize], 0, SendBufferSize);
            sendAsyncEventArgs.Completed += SendCompleted;
            handler.SendImplement += buffer =>
            {
                var bytesSend = buffer.Take(sendAsyncEventArgs.Buffer);
                sendAsyncEventArgs.SetBuffer(0, bytesSend);
                if (!socket.SendAsync(sendAsyncEventArgs))
                {
                    SendCompleted(socket, sendAsyncEventArgs);
                }

                return bytesSend;
            };
            // receive callback
            var receiveAsyncEventArgs = new SocketAsyncEventArgs {UserToken = handler};
            receiveAsyncEventArgs.SetBuffer(receiveBuffer, 0, ReceiveBufferSize);
            receiveAsyncEventArgs.Completed += ReceiveCompleted;
            BeginReceive(socket, receiveAsyncEventArgs);

            try
            {
                handler.IsWritable = true;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to change writable of handler {0}, disconnect...", e);
                OnDisconnected(socket, handler);
            }
        }

        /// <summary>
        /// The socket has already disconnected, but socket can be reused.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="handler"></param>
        protected virtual void OnDisconnected(Socket socket, T handler)
        {
            socket.Disconnect(true);

            try
            {
                handler.IsWritable = false;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to change writable of handler {0}", e);
            }
        }

        private void BeginReceive(Socket socket, SocketAsyncEventArgs e)
        {
            if (!socket.ReceiveAsync(e))
            {
                ReceiveCompleted(socket, e);
            }
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (sender is Socket socket && e.UserToken is T handler)
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    Logger.LogDebug("Thread [{0}], Received {1} bytes data", Thread.CurrentThread.ManagedThreadId,
                        e.BytesTransferred);
                    try
                    {
                        ReadOnlySpan<byte> buffer = e.Buffer;
                        handler.OnReceive(buffer.Slice(0, e.BytesTransferred));
                    }
                    catch (Exception exception)
                    {
                        Logger.LogError($"Socket connection {socket.RemoteEndPoint} has an exception: {exception}");
                        socket.Close();
                        return;
                    }

                    BeginReceive(socket, e);
                }
                else
                {
                    Logger.LogInformation(socket.Connected
                        ? $"Socket connection {socket.RemoteEndPoint} closed with {e.SocketError}"
                        : $"Socket closed with {e.SocketError}");
                    OnDisconnected(socket, handler);
                }
            }
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (sender is Socket socket && e.SocketError == SocketError.Success && e.UserToken is T handler)
            {
                try
                {
                    handler.OnSent(e.BytesTransferred);
                }
                catch (Exception exception)
                {
                    Logger.LogError($"Socket connection {socket.RemoteEndPoint} has an exception: {exception}");
                    socket.Close();
                }
            }
        }
    }
}