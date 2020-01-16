using System;
using RoyLab.ThunderSocket.Core.Handlers;
using RoyLab.ThunderSocket.Core.Mux;

namespace RoyLab.ThunderSocket.Core
{
    public class Application
    {
        private readonly TcpClient<DummyIOHandler> client;
        private readonly TcpServer<EchoIOHandler> server;

        public Application(TcpServer<EchoIOHandler> server, TcpClient<DummyIOHandler> client)
        {
            this.client = client;
            this.server = server;
        }

        public void Run()
        {
            while (true)
            {
                var line = Console.ReadLine();
                switch (line)
                {
                    case "start server":
                        server.Start();
                        break;

                    case "stop server":
                        server.Stop();
                        break;

                    case "start client":
                        client.Start();
                        break;

                    case "stop client":
                        client.Stop();
                        break;

                    case "quit":
                        return;
                }
            }
        }
    }
}