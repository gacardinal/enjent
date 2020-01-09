using System;

namespace NarcityMedia.Enjent.Server
{
    /// <summary>
    /// Represents an exception that occured in the WebSocketServer class logic
    /// </summary>
    public class WebSocketServerException : Exception
    {
        /// <summary>
        /// Creates a new instance of WebSocketServerException
        /// </summary>
        /// <param name="message">Message describing the exception</param>
        public WebSocketServerException(string message) : base(message)
        {
        }

        /// <summary>
        /// /// Creates a new instance of WebSocketServerException
        /// </summary>
        /// <param name="message">Message describing the exception</param>
        /// <param name="innerException">Inner exception that is at the origin of the current WebSocketServerException</param>
        public WebSocketServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents an exception that occured during the WebSocket negotiation
    /// </summary>
    public class WebSocketNegotiationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of WebSocketNegotiationException
        /// </summary>
        /// <param name="message">Message describing the exception</param>
        public WebSocketNegotiationException(string message) : base(message)
        {
        }

        /// <summary>
        /// /// Initializes a new instance of WebSocketNegotiationException
        /// </summary>
        /// <param name="message">Message describing the exception</param>
        /// /// <param name="innerException">Inner exception that is at the origin of the current WebSocketNegotiationException</param>
        public WebSocketNegotiationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}