using System;
using System.Threading;
using System.Collections.Generic;
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
        private ConcurrentQueue<WebSocketServerEvent> EventQueue;

        /// <summary>
        /// Wait handle that is set whenever anything is pushed to the EventQueue to signal <see cref="this.EventHandler" />
        /// to execute the events that are in the event queue.
        /// </summary>
        private ManualResetEventSlim handleMessageResetEvent;

        private void EventHandlerLoop()
        {
            IEnumerator<WebSocketServerEvent> eventsEnum;
            while (this.Listening)
            {
                handleMessageResetEvent.Wait();
                
                eventsEnum = EventQueue.GetEnumerator();

                if (eventsEnum.MoveNext())
                {
                    do
                    {
                        
                    }
                    while (eventsEnum.MoveNext());
                }

                handleMessageResetEvent.Reset();
            }
        }
    }
    
    /// <summary>
    /// Instance of this class are passed to WebSocketServerEvent handlers as arguments
    /// </summary>
    public abstract class WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public TWebSocketClient Cli;

        public WebSocketServerEventArgs(TWebSocketClient cli)
        {
            this.Cli = cli;
        }
    }
    
    public class WebSocketServerMessageEventArgs<TWebSocketClient> : WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public WebSocketDataFrame DataFrame;
        
        public WebSocketServerMessageEventArgs(TWebSocketClient cli, WebSocketDataFrame dataFrame) : base(cli)
        {
            this.DataFrame = dataFrame;
        }
    }

    public class WebSocketServerConnectionEventArgs<TWebSocketClient> : WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public WebSocketServerConnectionEventArgs(TWebSocketClient cli) : base(cli)
        {
        }
    }

    
    public class WebSocketServerDisconnectionEventArgs<TWebSocketClient> : WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        /// <summary>
        /// <see cref="Exception" /> that lead to the closing of the current conneciton, if any
        /// </summary>
        public Exception? Exception;

        public WebSocketServerDisconnectionEventArgs(TWebSocketClient cli) : base(cli)
        {
        }
    }

    public class WebSocketServerErrorEventArgs<TWebSocketClient> : WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        /// <summary>
        /// The <see cref="Exception" /> that triggered the current event
        /// </summary>
        public Exception Exception;

        public WebSocketServerErrorEventArgs(TWebSocketClient cli, Exception innerException) : base(cli)
        {
            this.Exception = innerException;
        }
    }

    public class WebSocketServerControlFrameEventArgs<TWebSocketClient> : WebSocketServerEventArgs<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        WebSocketControlFrame ControlFrame;

        public WebSocketServerControlFrameEventArgs(TWebSocketClient cli, WebSocketControlFrame cf) : base(cli)
        {
            this.ControlFrame = cf;
        }
    }

}
