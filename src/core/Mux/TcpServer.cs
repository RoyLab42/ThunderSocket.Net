using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;

namespace RoyLab.ThunderSocket.Core.Mux
{
    public class TcpServer<T> : AbstractTcpMux<T> where T : AbstractIOHandler
    {
        private readonly IPEndPoint serverEndPoint;
        private readonly SocketAsyncEventArgs serverSocketAsyncEventArgs;
        private Socket serverSocket;

        public TcpServer(ILogger<TcpServer<T>> logger, IOHandlerFactory<T> handlerFactory, IPEndPoint serverEndPoint)
            : base(logger, handlerFactory)
        {
            this.serverEndPoint = serverEndPoint;
            serverSocketAsyncEventArgs = new SocketAsyncEventArgs();
            serverSocketAsyncEventArgs.Completed += AcceptCompleted;
        }

        public override void Start()
        {
            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(serverEndPoint);
            serverSocket.Listen(5);

            Logger.LogInformation("TcpServer started, now listening on {0}", serverEndPoint);
            Accept();
        }

        public override void Stop()
        {
            serverSocket.Close();
        }

        protected override void OnDisconnected(Socket socket, T handler)
        {
            base.OnDisconnected(socket, handler);
            // client socket disconnected, close is and release resources (no need reuse client socket)
            socket.Close();
        }

        private void Accept()
        {
            Logger.LogDebug("TcpServer waiting for new connections, thread id: {0}",
                Thread.CurrentThread.ManagedThreadId);
            serverSocketAsyncEventArgs.AcceptSocket = null;
            if (!serverSocket.AcceptAsync(serverSocketAsyncEventArgs))
            {
                AcceptCompleted(this, serverSocketAsyncEventArgs);
            }
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var socket = e.AcceptSocket;
                Logger.LogInformation("New connection from: {0}", socket.RemoteEndPoint);
                OnConnected(socket);
                Accept();
            }
            else
            {
                Logger.LogError($"server socket read result: {e.SocketError}");
            }
        }
    }
}