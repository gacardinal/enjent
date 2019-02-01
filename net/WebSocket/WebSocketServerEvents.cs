using System;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public event WebSocketServerEvent OnConnect;
        public event WebSocketServerEvent OnDisconnect;
        public event WebSocketServerEvent OnMessage;
        public event WebSocketServerEvent OnControlFrame;
        public event WebSocketServerEvent OnError;

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs<TWebSocketClient> a);

        /// <summary>
        /// 
        /// </summary>
        public class WebSocketServerEventArgs<TWebSocketServerGeneric> where TWebSocketServerGeneric : TWebSocketClient
        {
            public TWebSocketClient Cli;
            public Exception Exception;
            public SocketDataFrame DataFrame;

            public WebSocketServerEventArgs(TWebSocketClient cli)
            {
                this.Cli = cli;
            }

            public WebSocketServerEventArgs(TWebSocketClient cli, SocketDataFrame dataFrame) : this(cli)
            {
                this.DataFrame = dataFrame;
            }

            public WebSocketServerEventArgs(TWebSocketClient cli, Exception innerException) : this(cli)
            {
                this.Exception = innerException;
            }
        }
    }
}
