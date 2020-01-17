using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;

namespace RoyLab.ThunderSocket.Core.Mux
{
    public class TcpClient<T> : AbstractTcpMux<T> where T : AbstractIOHandler
    {
        /// <summary>
        /// the client will retry every 10 seconds if:
        ///   1. it failed to connect to server
        ///   2. the connection with server was closed
        /// </summary>
        private const int ReconnectTimeDelay = 10;

        private readonly SocketAsyncEventArgs connectAsyncEventArgs;
        private readonly IReadOnlyList<EndPoint> endPoints;

        private Socket clientSocket;
        private int connectAttempts;
        private bool isShutdown;

        public TcpClient(ILogger<TcpClient<T>> logger, IOHandlerFactory<T> handlerFactory,
            IReadOnlyList<EndPoint> endPoints) : base(logger, handlerFactory)
        {
            this.endPoints = endPoints;
            connectAsyncEventArgs = new SocketAsyncEventArgs();
            connectAsyncEventArgs.Completed += ConnectCompleted;
        }

        public override void Start()
        {
            isShutdown = false;
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            Connect();
        }

        public override void Stop()
        {
            isShutdown = true;
            clientSocket.Shutdown(SocketShutdown.Both);
        }

        protected override void OnDisconnected(Socket socket, T handler)
        {
            base.OnDisconnected(socket, handler);
            if (!isShutdown)
            {
                Logger.LogInformation($"socket was closed from the server, reconnect in {ReconnectTimeDelay} seconds.");
                Thread.Sleep(ReconnectTimeDelay * 1000);
                connectAttempts = 0;
                Connect();
            }
            else
            {
                Logger.LogDebug("socket has disconnected, now release all associated resources.");
                clientSocket.Close();
            }
        }

        private void Connect()
        {
            var connectedToIndex = connectAttempts % endPoints.Count;
            Logger.LogInformation(
                $"Connecting to {endPoints[connectedToIndex]} with attempt count: {connectAttempts + 1}");

            connectAsyncEventArgs.RemoteEndPoint = endPoints[connectedToIndex];
            if (!clientSocket.ConnectAsync(connectAsyncEventArgs))
            {
                ConnectCompleted(clientSocket, connectAsyncEventArgs);
            }
        }

        private void ConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Logger.LogInformation($"Connected to {e.RemoteEndPoint}");
                OnConnected(sender as Socket);
            }
            else
            {
                Logger.LogError(
                    $"Failed to connect {e.RemoteEndPoint} with error {e.SocketError}, retry in {ReconnectTimeDelay} seconds.");
                Thread.Sleep(ReconnectTimeDelay * 1000);
                connectAttempts++;
                Connect();
            }
        }
    }
}