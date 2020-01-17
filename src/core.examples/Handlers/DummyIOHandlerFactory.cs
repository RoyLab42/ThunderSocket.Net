using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public class DummyIOHandlerFactory : IOHandlerFactory<DummyIOHandler>
    {
        private readonly ILogger<DummyIOHandler> logger;

        public DummyIOHandlerFactory(ILogger<DummyIOHandler> logger)
        {
            this.logger = logger;
        }

        public DummyIOHandler CreateNewHandler()
        {
            return new DummyIOHandler(logger);
        }
    }
}