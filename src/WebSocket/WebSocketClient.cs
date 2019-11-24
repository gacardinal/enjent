using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace NarcityMedia.Enjent
{
    /// <summary>
    /// Represents a client a.k.a. an end user that connected to the server by the HTTP protocol
    /// and that has subsequently upgraded to the WebSocket protocol.
    /// </summary>
    public partial class WebSocketClient : IDisposable
    {
        /// <summary>
        /// Unix timestamp (32 bits unsigned) that represents the time at which the current object was created
        /// </summary>
        public readonly DateTime InitTime;

        /// <summary>
        /// Unique identifier for the client
        /// </summary>
        public readonly Guid Id;
        
        /// <summary>
        /// The TCP socket associated with the current WebSocketClient
        /// </summary>
        public Socket Socket;

        /// <summary>
        /// Represents the initial HTTP request that was used to negotiate the WebSocket connection
        /// </summary>
        public EnjentHTTPRequest? InitialRequest;

        /// <summary>
        /// Initializes a new instance of a WebSocketClient
        /// </summary>
        /// <param name="socket">A connected TCP socket that represents a conneciton to a remote client</param>
        public WebSocketClient(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("socket");

            this.Socket = socket;
            this.Id = Guid.NewGuid();
            this.InitTime = DateTime.Now;
        }

        public void Dispose()
        {
            if (this.Socket != null)
            {
                // Close method does Dispose of the object
                this.Socket.Close();
            }
        }
    }
}
