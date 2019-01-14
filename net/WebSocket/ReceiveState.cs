using System;
using System.Net.Sockets;

namespace NarcityMedia.Net
{
    /// <summary>
    /// An instance of this class serves as a state object passed to a spcket's
    /// BeginReceive() method to achieve asynchronous I/O
    /// </summary>
    internal class ReceiveState
    {
        public Socket socket;
        public const int INIT_BUFFER_SIZE = 2;
        public byte[] buffer = new byte[INIT_BUFFER_SIZE];
    }
}