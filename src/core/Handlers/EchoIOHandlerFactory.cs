using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public class EchoIOHandlerFactory : IOHandlerFactory<EchoIOHandler>
    {
        private readonly ILogger<EchoIOHandler> logger;

        public EchoIOHandlerFactory(ILogger<EchoIOHandler> logger)
        {
            this.logger = logger;
        }

        public EchoIOHandler CreateNewHandler()
        {
            return new EchoIOHandler(logger);
        }
    }
}