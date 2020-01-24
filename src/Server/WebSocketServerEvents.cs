using System;
using System.Threading;
using System.Collections.Concurrent;

using NarcityMedia.Enjent.WebSocket;

namespace NarcityMedia.Enjent.Server
{
    public partial class WebSocketServerCore<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        public delegate void ConnectionEvent(object sender, ConnectionEventArgs ca);
        public delegate void DisconnectEvent(object sender, DisconnectionEventArgs da);
        public delegate void MessageEvent(object sender, MessageEventArgs ma);
        public delegate void ControlFrameReceived(object sender, ControlFrameEventArgs a);
        public delegate void ErrorEvent(object sender, ErrorEventArgs a);

        private ConnectionEvent? _onConnect				= delegate {};
        private DisconnectEvent? _onDisconnect			= delegate {};
        private MessageEvent? _onMessage				= delegate {};
        private ControlFrameReceived? _onControlFrame 	= delegate {};
        private ErrorEvent? _onError					= delegate {};

        // Since event can be set to null references by the -= operator, they need to be declared as nullable
        // For better void safety. However, this poses a problem when we try to lock them.
        // At the same time, we don't want to have one single lock for all the references because we don't want a thread
        // subscribing to one event to block others from subscribing to another event
        private object onConnectEventMutex      = new object();
        private object onDisconnectEventMutex   = new object();
        private object onMessageEventMutex      = new object();
        private object onControlFrameEventMutex = new object();
        private object onErrorEventMutex        = new object();

        public event ConnectionEvent OnConnect 
        {
            add { lock ( this.onConnectEventMutex) { this._onConnect += value; } }
            remove { lock ( this.onConnectEventMutex) { this._onConnect -= value; } }
        }
        public event DisconnectEvent OnDisconnect 
        {
            add { lock (this.onDisconnectEventMutex) { this._onDisconnect += value; } }
            remove { lock (this.onDisconnectEventMutex) { this._onDisconnect -= value; } }
        }
        public event MessageEvent OnMessage 
        {
            add { lock (this.onMessageEventMutex) { this._onMessage += value; } }
            remove { lock (this.onMessageEventMutex) { this._onMessage -= value; } }
        }
        public event ControlFrameReceived OnControlFrame 
        {
            add { lock (this.onControlFrameEventMutex) { this._onControlFrame += value; } }
            remove { lock (this.onControlFrameEventMutex) { this._onControlFrame -= value; } }
        }
        public event ErrorEvent OnError 
        {
            add { lock (this.onErrorEventMutex) { this._onError += value; } }
            remove { lock (this.onErrorEventMutex) { this._onError -= value; } }
        }

        /// <summary>
        /// Single thread responsible for executing the client code that handles the events
        /// that are awaiting to be processed in the EventQueue
        /// </summary>
        private Thread EventHandler;

        /// <summary>
        /// Thread safe queue to hold the event args that correspond to events
        /// that need to be processed by the EventHandler thread
        /// </summary>
        private ConcurrentQueue<WebSocketServerEventArgs> EventQueue;

        /// <summary>
        /// Wait handle that is set whenever anything is pushed to the EventQueue to signal <see cref="this.EventHandler" />
        /// to execute the events that are in the event queue.
        /// </summary>
        private ManualResetEventSlim handleMessageResetEvent;

        /// <summary>
        /// Listens to <see cref="this.handleMessageResetEvent" /> to know when events are pushed in <see cref="this.EventQueue" />.
        /// When the ManualResetEvent is signaled, this methods empties the event queue and processes the events sequentially and
        /// goes back to waiting for the ManualResetEvent
        /// </summary>
        /// <remarks>
        /// This method is executed by the <see cref="this.EventHandler" /> thread
        /// </remarks>
        private void EventHandlerLoop()
        {
            WebSocketServerEventArgs? curEventArgs;
            while (this.Listening)
            {
                // After loop starts, wait for manual reset event to be set, indicating events are ready to be processed
                handleMessageResetEvent.Wait();
                
                if (!EventQueue.IsEmpty)
                {
                    // Execute all avents sequentially until EventQueue is empty
                    do
                    {
						if (EventQueue.TryDequeue(out curEventArgs))
						{
							// Compare Enum values instead of trying to perform a cast for each type of event
							// reference is only cast when proper type is found
							if (curEventArgs.EvType == EventType.Connection) {
								if (this._onConnect != null) this._onConnect.Invoke(this, (ConnectionEventArgs) curEventArgs);
							} else if (curEventArgs.EvType == EventType.Message) {
								if (this._onMessage != null) this._onMessage.Invoke(this, (MessageEventArgs) curEventArgs);
							} else if (curEventArgs.EvType == EventType.Disconnection) {
								if (this._onDisconnect != null) this._onDisconnect.Invoke(this, (DisconnectionEventArgs) curEventArgs);
							} else if (curEventArgs.EvType == EventType.ControlFrame) {
								if (this._onControlFrame != null) this._onControlFrame.Invoke(this, (ControlFrameEventArgs) curEventArgs);
							} else if (curEventArgs.EvType == EventType.Error) {
								if (this._onError != null) this._onError.Invoke(this, (ErrorEventArgs) curEventArgs);
							}
						}
						curEventArgs = null;
                    }
                    while (!EventQueue.IsEmpty);
                }

                // After EventQueue has been emptied, reset the manual reset event to block thread until
                // some new events are pushed in the queue
                handleMessageResetEvent.Reset();
            }
        }

        /// <summary>
        /// Pushes a given instance of a type derived of WebSocketServerEventArgs in the EventQueue and sets the
        /// ManualResetEvent to signal the EventHandler thread to proceed
        /// </summary>
        /// <remarks>
        /// This code will be executed by a large number of different ThreadPool threads that are responsible for handling
        /// traffic on all the different websocket TCP sockets that are open with the clients
        /// </remarks>
        protected void PushToEventQueue(WebSocketServerEventArgs eventArgs)
        {
            EventQueue.Enqueue(eventArgs);
            this.handleMessageResetEvent.Set();
        }
        
        /// <summary>
        /// Instance of this class are passed to event handlers as arguments
        /// </summary>
        public abstract class WebSocketServerEventArgs : EventArgs
        {

			public EventType EvType;

            public TWebSocketClient Cli;

            public WebSocketServerEventArgs(TWebSocketClient cli) : base()
            {
                this.Cli = cli;
            }
        }
        
        /// <summary>
        /// Instance of this class are passed to <see cref="this.MessageEvent" /> handlers when a message is sent from a client
        /// to the current server
        /// </summary>
        public abstract class MessageEventArgs : WebSocketServerEventArgs
        {
            public object? Message;
            
            public MessageEventArgs(TWebSocketClient cli, object message) : base(cli)
            {
                this.Message = message;
				this.EvType = EventType.Message;
            }
        }

		public class BinaryMessageEventArgs : MessageEventArgs
		{
			public new BinaryMessage Message;

			public BinaryMessageEventArgs(TWebSocketClient cli, BinaryMessage message) : base(cli, message)
			{
				this.Message = message;
			}
		}

		public class TextMessageEventArgs : MessageEventArgs
		{
			public new TextMessage Message;
		
			public TextMessageEventArgs(TWebSocketClient cli, TextMessage message) : base(cli, message)
			{
				this.Message = message;
			}
		}

        /// <summary>
        /// Instance of this class are passed to <see cref="this.ConnectionEvent" /> handlers when a client successfully connects
        /// to the current server
        /// </summary>
        public class ConnectionEventArgs : WebSocketServerEventArgs
        {
            public ConnectionEventArgs(TWebSocketClient cli) : base(cli)
            {
				this.EvType = EventType.Connection;
            }
        }

        /// <summary>
        /// Instance of this class are passed to <see cref="this.DisconnectEvent" /> handlers when a client disconnects
        /// to the current server
        /// </summary>
        public class DisconnectionEventArgs : WebSocketServerEventArgs
        {
            /// <summary>
            /// <see cref="Exception" /> that lead to the closing of the current conneciton, if any
            /// </summary>
            public Exception? Exception;

			public WebSocketCloseFrame? CloseFrame;

            public DisconnectionEventArgs(TWebSocketClient client) : base(client)
            {
				this.EvType = EventType.Disconnection;
			}

			public DisconnectionEventArgs(TWebSocketClient client, WebSocketCloseFrame closeFrame) : this(client)
			{
				this.CloseFrame = closeFrame;
			}

            public DisconnectionEventArgs(TWebSocketClient client, Exception e) : this(client)
            {
                Exception = e;
            }
        }

        /// <summary>
        /// /// Instance of this class are passed to <see cref="this.ConnectionEvent" /> handlers when an internal error occurs in the current server.
        /// When this event is fired
        /// </summary>
        public class ErrorEventArgs : WebSocketServerEventArgs
        {
            /// <summary>
            /// The <see cref="WebSocketServerException" /> that triggered the current event
            /// </summary>
            public WebSocketServerException Exception;

            public ErrorEventArgs(TWebSocketClient cli, WebSocketServerException innerException) : base(cli)
            {
                this.Exception = innerException;
				this.EvType = EventType.Error;
            }
        }

        public class ControlFrameEventArgs : WebSocketServerEventArgs
        {
            WebSocketControlFrame ControlFrame;

            public ControlFrameEventArgs(TWebSocketClient cli, WebSocketControlFrame cf) : base(cli)
            {
                this.ControlFrame = cf;
				this.EvType = EventType.ControlFrame;
            }
        }
    }

    public enum EventType {
        Connection,
        Disconnection,
        Message,
        ControlFrame,
        Error
    }
}
