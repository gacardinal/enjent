using System;

namespace NarcityMedia.Net
{

    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

        public event WebSocketServerEvent OnConnect;
        public event WebSocketServerEvent OnDisconnect;
        public event WebSocketServerEvent OnMessage;
        public event WebSocketServerEvent OnControlFrame;
        public event WebSocketServerEvent OnError;

        /// <summary>
        /// 
        /// </summary>
        public class WebSocketServerEventArgs
        {
            public TWebSocketClient Cli;
            public Exception Exception;
            public WebSocketDataFrame DataFrame;

            public WebSocketServerEventArgs(TWebSocketClient cli)
            {
                this.Cli = cli;
            }

            public WebSocketServerEventArgs(TWebSocketClient cli, WebSocketDataFrame dataFrame) : this(cli)
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
