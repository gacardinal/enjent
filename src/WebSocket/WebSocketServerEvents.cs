using System;
using System.Threading;
using System.Collections.Concurrent;

namespace NarcityMedia.Enjent
{

    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        private WebSocketServerEvent _onConnect = delegate {};
        private WebSocketServerEvent _onDisconnect = delegate {};
        private WebSocketServerEvent _onMessage = delegate {};
        private WebSocketServerEvent _onControlFrame = delegate {};
        private WebSocketServerEvent _onError = delegate {};

        public delegate void WebSocketServerEvent(object sender, WebSocketServerEventArgs a);

        public event WebSocketServerEvent OnConnect 
        {
            add { lock ( this._onConnect) { this._onConnect += value; } }
            remove { lock ( this._onConnect) { this._onConnect -= value; } }
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

        /// <summary>
        /// Single thread responsible for executing the client code that handles the events
        /// that are awaiting to be processed in the EventQueue
        /// </summary>
        private Thread EventHandler;

        /// <summary>
        /// Thread safe queue to hold the events that are dispatched and awaiting to be processed
        /// by the EventHandler thread
        /// </summary>
        private ConcurrentQueue<WebSocketServerEventArgs> EventQueue;

        private ManualResetEvent handleMessageResetEvent;

        private void EventHandlerLoop()
        {
            while (this.Listening)
            {
                
            }
        }

        /// <summary>
        /// Instance of this class are passed to WebSocketServerEvent handlers as arguments
        /// </summary>
        public class WebSocketServerEventArgs
        {
            public TWebSocketClient Cli;
            public Exception? Exception;
            public WebSocketDataFrame? DataFrame;

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
