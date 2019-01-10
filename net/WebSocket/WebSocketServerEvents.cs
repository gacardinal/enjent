using System;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer
    {
        public event WebSocketServerEvent OnConnect;
        public event WebSocketServerEvent OnDisconnect;
        public event WebSocketServerEvent OnMessage;
        public event WebSocketServerEvent OnControlFrame;
        public event WebSocketServerEvent OnError;

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

    }

    /// <summary>
    /// 
    /// </summary>
    public class WebSocketServerEventArgs
    {
        public WebSocketClient Cli;
        public Exception Exception;
        public SocketDataFrame DataFrame;

        public WebSocketServerEventArgs(WebSocketClient cli)
        {
            this.Cli = cli;
        }

        public WebSocketServerEventArgs(WebSocketClient cli, SocketDataFrame dataFrame) : this(cli)
        {
            this.DataFrame = dataFrame;
        }

        public WebSocketServerEventArgs(Exception innerException)
        {
            this.Exception = innerException;
        }
    }
}
