using Microsoft.Extensions.Logging;
using RoyLab.ThunderSocket.Core.Handlers.Interfaces;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public class ComplexMessageIOHandlerFactory : IOHandlerFactory<ComplexMessageIOHandler>
    {
        private readonly ILogger<ComplexMessageIOHandler> logger;

        public ComplexMessageIOHandlerFactory(ILogger<ComplexMessageIOHandler> logger)
        {
            this.logger = logger;
        }

        public ComplexMessageIOHandler CreateNewHandler()
        {
            return new ComplexMessageIOHandler(logger);
        }
    }
}