using System;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer
    {
        public event WebSocketServerEvent OnConnect;
        public event WebSocketServerEvent OnDisconnect;
        public event WebSocketServerEvent OnMessage;
        public event WebSocketServerEvent OnError;

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

    }

    /// <summary>
    /// 
    /// </summary>
    public class WebSocketServerEventArgs
    {
        public WebSocketClient cli;
        public Exception exception;

        public WebSocketServerEventArgs(WebSocketClient cli)
        {
            this.cli = cli;
        }

        public WebSocketServerEventArgs(Exception innerException)
        {
            this.exception = innerException;
        }
    }
}
