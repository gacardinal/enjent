using System;

namespace NarcityMedia.Enjent
{

    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        private WebSocketServerEvent _onConnect;
        private WebSocketServerEvent _onDisconnect;
        private WebSocketServerEvent _onMessage;
        private WebSocketServerEvent _onControlFrame;
        private WebSocketServerEvent _onError;

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

        public event WebSocketServerEvent OnConnect 
        {
            add { lock (this._onConnect) { this._onConnect += value; } }
            remove { lock (this._onConnect) { this._onConnect -= value; } }
        }
        public event WebSocketServerEvent OnDisconnect 
        {
            add { lock (this._onDisconnect) { this._onDisconnect += value; } }
            remove { lock (this._onDisconnect) { this._onDisconnect -= value; } }
        }
        public event WebSocketServerEvent OnMessage 
        {
            add { lock (this._onMessage) { this._onMessage += value; } }
            remove { lock (this._onMessage) { this._onMessage -= value; } }
        }
        public event WebSocketServerEvent OnControlFrame 
        {
            add { lock (this._onControlFrame) { this._onControlFrame += value; } }
            remove { lock (this._onControlFrame) { this._onControlFrame -= value; } }
        }
        public event WebSocketServerEvent OnError 
        {
            add { lock (this._onError) { this._onError += value; } }
            remove { lock (this._onError) { this._onError -= value; } }
        }

        public void TryInvokeEach(WebSocketServerEvent ev, object sender, WebSocketServerEventArgs args)
        {
            foreach (WebSocketServerEvent evHandler in ev.GetInvocationList())
            {
                try
                {
                    evHandler(sender, args);
                }
                catch (Exception e)
                {
                    // TODO Handle exception
                }
            }
        }

        /// <summary>
        /// Instance of this class are passed to WebSocketServerEvent handlers as arguments
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
