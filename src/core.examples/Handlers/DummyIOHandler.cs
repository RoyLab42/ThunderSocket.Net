using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace RoyLab.ThunderSocket.Core.Handlers
{
    public class DummyIOHandler : AbstractIOHandler
    {
        private bool isWritable;

        internal DummyIOHandler(ILogger<DummyIOHandler> logger) : base(logger)
        {
        }

        internal override bool IsWritable
        {
            get => isWritable;
            set
            {
                if (isWritable != value)
                {
                    isWritable = value;
                    if (isWritable)
                    {
                        OnConnected();
                    }
                }
            }
        }

        private void OnConnected()
        {
            for (var i = 0; i < 100; i++)
            {
                SendInternal(Encoding.UTF8.GetBytes($"hello world! [{i}]"));
                Thread.Sleep(5000);
            }
        }
    }
}