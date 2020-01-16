using System;

namespace RoyLab.ThunderSocket.Core.Handlers.Interfaces
{
    public interface IOHandler
    {
        /// <summary>
        /// called by the io thread to buffer received data
        /// </summary>
        /// <param name="data"></param>
        void OnReceive(ReadOnlySpan<byte> data);

        /// <summary>
        /// reset for reuse
        /// </summary>
        void Reset();
    }
}