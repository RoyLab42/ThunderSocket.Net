using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers;
using RoyLab.ThunderSocket.Core.Mux;

namespace RoyLab.ThunderSocket.Core
{
    internal static class Program
    {
        private static void Main()
        {
            var services = new ServiceCollection();

            services.AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(cfg => cfg.MinLevel = LogLevel.Debug)
                .AddTransient<Application>();

            services.AddScoped<EchoIOHandlerFactory>();
            services.AddScoped(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<TcpServer<EchoIOHandler>>>();
                var handlerFactory = serviceProvider.GetService<EchoIOHandlerFactory>();
                return new TcpServer<EchoIOHandler>(logger, handlerFactory, new IPEndPoint(IPAddress.Any, 8000));
            });

            services.AddScoped<DummyIOHandlerFactory>();
            services.AddScoped(serviceProvider =>
            {
                var dummyIOHandlerLogger = serviceProvider.GetService<ILogger<TcpClient<DummyIOHandler>>>();
                var dummyIOHandlerFactory = serviceProvider.GetService<DummyIOHandlerFactory>();
                return new TcpClient<DummyIOHandler>(dummyIOHandlerLogger, dummyIOHandlerFactory,
                    new[] {new IPEndPoint(IPAddress.Loopback, 8000)});
            });

            using (var serviceProvider = services.BuildServiceProvider())
            {
                var app = serviceProvider.GetService<Application>();
                app.Run();
            }
        }
    }
}