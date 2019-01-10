using System;

namespace NarcityMedia.Net
{
    public class WebSocketServerException : Exception
    {
        public WebSocketServerException(string message) : base(message)
        {
        }

        public WebSocketServerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class WebSocketNegotiationException : Exception
    {

        public WebSocketNegotiationException(string message) : base(message)
        {
        }

        public WebSocketNegotiationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}