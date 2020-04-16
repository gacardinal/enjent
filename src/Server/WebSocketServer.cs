using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using NarcityMedia.Enjent.WebSocket;

namespace NarcityMedia.Enjent.Server
{
    /// <summary>
    /// Closed constructed version of the WebSocketServer class
    /// </summary>
    /// <typeparam name="WebSocketClient">Default provided type for the client ovject</typeparam>
    /// <remark>
    /// When using this closed constructed version derived from the generic <see cref="WebSocketServer<TWebSocketClient>" /> class,
    /// it is not necessary to provide a custom client initialization strategy because one is provided by the current class.
    public partial class WebSocketServer : WebSocketServer<WebSocketClient>
    {
        /// <summary>
        /// Initializes a new instance of the WebSocketServer class
        /// </summary>
        public WebSocketServer() : base(DefaultInitializationStrategy)
        {
        }
        
        /// <summary>
        /// Default client initialization strategy
        /// </summary>
        /// <param name="socket">The newly accepted WebSocket connection</param>
        /// <param name="initialWSRequet">The HTTP request that initiated the WebSocket connection</param>
        private static WebSocketClient DefaultInitializationStrategy(Socket socket, EnjentHTTPRequest initialWSRequet)
        {
            WebSocketClient cli = new WebSocketClient(socket);
            cli.InitialRequest = initialWSRequet;

            return cli;
        }
    }

    public class WebSocketServer<TWebSocketClient> : WebSocketServerCore<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        /// <summary>
        /// Initializes a new instance of the WebSocketServer class
        /// </summary>
        public WebSocketServer(ClientInitialization initStrat) : base(initStrat)
        {
        }
    }

    /// <summary>
    /// Provides server-side WebSocket functionnalities
    /// </summary>
    /// <typeparam name="TWebSocketClient">The type used to represent clients that connect to the current WebSocketServer</typeparam>
    public abstract partial class WebSocketServerCore<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        /// <summary>
        /// Thread that will listen for incoming connections
        /// </summary>
        private Thread Listener;

        /// <summary>
        /// The socket that will be bound on the endpoint passed to the WebSocketServer.Start() method
        /// </summary>
        private Socket Socket;

        /// <summary>
        /// Indicates whether the server is listening for incoming connections
        /// </summary>
        private bool Listening;

        /// <summary>
        /// Holds a reference to every connected client
        /// </summary>
        private List<TWebSocketClient> clients;

		/// <summary>
		/// Represents a structure to organizes rooms in namespaces
		/// </summary>
		internal readonly WebSocketRoomDirectory<TWebSocketClient> RoomNamespaces;

        /// <summary>
        /// Strategy used to initialize a new instance of the generic TWebSocketClient type when a new WebSocket connection is accepted
        /// </summary>
        /// <remark>
        /// It is mandatory to supply an initialization strategy when using the generic <see cref=" WebSocketServer<TWebSocketClient>" /> class.
        /// If you do not wish to provide such a strategy to initialize a custom type, use the non generic version of this class <see cref="WebSocketServer" />.
        /// </remark>
        public delegate TWebSocketClient ClientInitialization(Socket socket, EnjentHTTPRequest initialWSRequest);
        private ClientInitialization ClientInitializationStrategy;

		static WebSocketServerCore()
		{
			GoingAwayFrameBytes = new WebSocketCloseFrame(WebSocketCloseCode.GoingAway).GetBytes();
		}

        /// <summary>
        /// Creates a new instance of the WebSocketServer class
        /// </summary>
        public WebSocketServerCore(ClientInitialization initStrategy)
        {
            this.Listener = new Thread(this.NegociationLoop);
            // Set Thread as foreground to prevent program execution finishing
            this.Listener.IsBackground = false;
            this.Listener.Name = "WebSocketServerHTTPListener";
            
            this.EventHandler = new Thread(this.EventHandlerLoop);
            this.EventHandler.Name = "WebSocketEventHandler";
            this.EventQueue = new ConcurrentQueue<WebSocketServerEventArgs>();

            this.handleMessageResetEvent = new ManualResetEventSlim(false);

			this.RoomNamespaces = new WebSocketRoomDirectory<TWebSocketClient>(this);

            this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Socket.ReceiveTimeout = 1000;

            this.clients = new List<TWebSocketClient>(1024);

            this.ClientInitializationStrategy = initStrategy;

            // this.OnDisconnect += DisconnectProcedure;
        }

        /// <summary>
        /// Starts listenning for incomming HTTP WebSocket connections
        /// </summary>
        /// <param name="endpoint">The endpoint at which the WebSocketServer should listen for incomming WebSocket HTTP connections</param>
        /// <exception cref="WebSocketServerException">Thros any exception that might occur while trying to bind the socket as an inner exception</exception>
        public void Start(IPEndPoint endpoint)
        {
            if (!this.Listening)
            {
                try
                {
                    this.Socket.Bind(endpoint);
                    this.Socket.Listen(1024);
                    this.Listening = true;
                    this.Listener.Start();
                    this.EventHandler.Start();
                }
                catch (SocketException e)
                {
                    throw new WebSocketServerException(String.Format("Couldn't bind on endpoint {0}:{1} the port might already be in use. SocketException ErrorCode: ", endpoint.Address, endpoint.Port, e.ErrorCode), e);
                }
                catch (OutOfMemoryException e)
                {
                    throw new WebSocketServerException("The WebSocketServer was unable to start a new Thread because the system was out of memory", e);
                }
                catch (Exception e)
                {
                    throw new WebSocketServerException("An error occured while binding the HTTP listener socket", e);
                }
            }
            else
            {
                throw new WebSocketServerException("Cannot start the server when it is already running", new InvalidOperationException());
            }
        }

		private static byte[] GoingAwayFrameBytes;

        /// <summary>
        /// Executed by the 'listener' Thread, used to perform cleanup operation before quitting
        /// </summary>
        private Task Quit()
        {
			WebSocketCloseFrame goingAwayFrame = new WebSocketCloseFrame(WebSocketCloseCode.GoingAway);
			byte[] frameBytes = goingAwayFrame.GetBytes();

			Action<object> sendCloseFrame = NotifyGoingAway;

			lock (this.clients)
			{
				Task[] tasks = new Task[this.clients.Count];
				for (int i = 0; i < this.clients.Count; i++)
				{
					tasks[i] = Task.Factory.StartNew(sendCloseFrame, this.clients[i]);
				}

				return Task.WhenAll(tasks);
			}
        }

		private static void NotifyGoingAway(object state)
		{
			TWebSocketClient? cli = state as TWebSocketClient;
			if (cli != null)
			{
				cli.Socket.Send(GoingAwayFrameBytes);
			}
		}

        /// <summary>
        /// Safely stops the current WebSocketServer
        /// </summary>
        public void Stop()
        {
            this.Listening = false;  // Listener Thread will exit when safe to do so
            try
            {
                this.Socket.Disconnect(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Default handler for WebSocket control frames that is invoked whenever the current WebSocketServer receives
        /// a WebSocket control frame
        /// </summary>
        /// <param name="cli">The client that sent the control frame</param>
        /// <param name="cFrame">The control frame that was received</param>
        private void DefaultControlFrameHandler(TWebSocketClient cli, WebSocketControlFrame cFrame)
        {
            switch (cFrame.OpCode)
            {
                case WebSocketOPCode.Close:
                    try
                    {
						var closeFrame = (WebSocketCloseFrame) cFrame;
						var echoCloseFrame = new WebSocketCloseFrame();
						var de = new DisconnectionEventArgs(cli);
						if (closeFrame.CloseCode != WebSocketCloseCode.NoCloseCode)
						{
							echoCloseFrame.CloseCode = closeFrame.CloseCode;
							de.CloseFrame = closeFrame;
						}
						this.SendControlFrame(cli, echoCloseFrame);
                        this.PushToEventQueue(de);
                    }
                    catch (Exception e)
                    {
                        WebSocketServerException ex = new WebSocketServerException("Error while sending 'close' control frame. Connection will be dropped forcefully. See inner exception for additional information.", e);
                        this.PushToEventQueue(new DisconnectionEventArgs(cli, ex));
                    }
                    finally
                    {
                        cli.Dispose();
                    }
                    break;
                case WebSocketOPCode.Ping:
                    try
                    {
						WebSocketPingFrame pf = (WebSocketPingFrame) cFrame;
						WebSocketPongFrame response = new WebSocketPongFrame();
						response.Payload = pf.Payload;
						this.SendControlFrame(cli, pf);
						StartClientReceive(cli);
					}
                    catch (Exception e)
                    {
                        WebSocketServerException ex = new WebSocketServerException("Error while sending 'pong' control frame", e);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Used to pass context to an Async operation
        /// </summary>
        private class SocketNegotiationState
        {
            /// <summary>
            /// The socket that is currently attempting to open a connection
            /// </summary>
            public Socket handler;

            /// <summary>
            /// The associated client object
            /// </summary>
            public TWebSocketClient? cli;
            /// <summary>
            /// Represents an exception that MIGHT have occured during the negotiation
            /// </summary>
            public WebSocketNegotiationException? exception;
            /// <summary>
            /// Handles a successful WebSocket negotiation
            /// </summary>
            /// <param name="cli">The newly created client object</param>
            public delegate void NegotiationCallback(TWebSocketClient? cli);
            /// <summary>
            /// Invoked once the WebSocket negotiation has completed
            /// </summary>
            public NegotiationCallback? done;

            /// <summary>
            /// Initializes a new instance of the SocketNegotiationState class
            /// </summary>
            /// <param name="handler">The socket used to negotiate the WebSocket connection</param>
            public SocketNegotiationState(Socket handler)
            {
                this.handler = handler;
            }
        }

        /// <summary>
        /// Executed by a dedicated Thread, in charge of listening for HTTP requests and handle WebSocket negociation
        /// </summary>
        private void NegociationLoop()
        {
            while (this.Listening)
            {
                Socket handler = this.Socket.Accept();  // Blocking
                SocketNegotiationState state = new SocketNegotiationState(handler);
                state.done = cli => {
                    // Executed async once the negotiation is done
                    if (state.exception == null && cli != null)
                    {
                        Exception? e = this.AddClient(cli);
                        if (e == null)
                        {
                            this.PushToEventQueue(new ConnectionEventArgs(cli));
                        }
                        else
                        {
                            this.PushToEventQueue(new ErrorEventArgs(cli, new WebSocketServerException("Error adding new client to internal list", e)));
                            cli.Dispose();
                            handler.Dispose();
                        }
                    }
                    else
                    {
                        // TODO: make this not so nasty
                        string response = "HTTP/1.1 431 Request Header Fields too large\nEnjent-Message: Request header fields were too large\n\nRequest header fields too large";
                        // TODO Handle socket error that can be thrown here
                        handler.Send(System.Text.Encoding.Default.GetBytes(response));
                        handler.Close();
                        handler.Dispose();
                    }
                };

                // 'WebSocketServerHTTPListener' threaad can move on and accept other requests
                ThreadPool.QueueUserWorkItem(this.NegotiateWebSocketConnection, state);
            }

            this.Quit();
            return;     // End 'listener' Thread execution
        }

        /// <summary>
        /// Adds a client to the connected clients collection in a thread safe manner
        /// </summary>
        /// <param name="cli">The client to add to <see cref="this.clients" />.</param>
        /// <remark>
        /// Will most likely be executed by a ThreadPool thread during the negotiation of the WebSocket connection
        /// </remark>
        /// <return>
        /// An instance of a class derived of <see cref="Exception" /> that represents an exception that occured
        /// while trying to add the client to the clients list or while attempting to listen.
        /// If no exception is thrown, null is returned
        /// </return>
        private Exception? AddClient(TWebSocketClient cli)
        {
            try
            {
                this.StartClientReceive(cli);

                lock (this.clients)
                {
                    this.clients.Add(cli);
                }
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }

        /// <summary>
        /// Removes a client from the connected clients collection in a thread safe manner
        /// </summary>
        /// <param name="cli">The client to remove from <see cref="this.clients" />.</param>
        /// <remark>
        /// Will most likely be executed by a ThreadPool thread which executes the receive logic
        /// </remark>
        private void RemoveClient(TWebSocketClient cli)
        {
            if (cli != null)
            {
                lock (this.clients)
                {
                    this.clients.Remove(cli);
                }
            }
        }

        /// <summary>
        /// Starts receiving data from a client in an asynchronous manner
        /// </summary>
        /// <param name="cli">The client from which to start receiving</param>
        private void StartClientReceive(TWebSocketClient cli)
        {
            ReceiveState receiveState = new ReceiveState(cli);
            receiveState.Cli.Socket.BeginReceive(receiveState.buffer, 0, ReceiveState.INIT_BUFFER_SIZE, 0,
                                    new AsyncCallback(ReceiveCallback), receiveState);
        }

        /// <summary>
        /// An instance of this class serves as a state object passed to a socket's
        /// BeginReceive() method to achieve asynchronous I/O
        /// </summary>
        private class ReceiveState
        {
            /// <summary>
            /// The client from which to receive data
            /// </summary>
            public TWebSocketClient Cli;
            /// <summary>
            /// Initial receiving buffer size
            /// </summary>
            /// <remark>
            /// The size of this buffer is two bcause WebSocket frame headers are no more than two bytes
            /// and there is no point in receiving more until the headers are analyzed
            /// </remark>
            public const int INIT_BUFFER_SIZE = 2;
            /// <summary>
            /// The buffer that holds the received data
            /// </summary>
            public byte[] buffer = new byte[INIT_BUFFER_SIZE];

            public ReceiveState(TWebSocketClient cli)
            {
                if (cli == null)
                    throw new ArgumentNullException("cli");

                this.Cli = cli;
            }
        }

		/// <summary>
		/// Represents a websocket frame buffer to handle fragmented messages for a single client.
		/// Each client's socket receive operations are handled by a single ThreadPool thread hence the buffer
		/// must be held in a ThreadLocal
		/// </summary>
		private ThreadLocal<WebSocketMessageBuffer?> tl_frameBuffer = new ThreadLocal<WebSocketMessageBuffer?>();

        /// <summary>
        /// Executes logic to receive data from a WebSocket connection
        /// </summary>
        /// <param name="iar">The result of the asynchronous receive operation</param>
        private void ReceiveCallback(IAsyncResult iar)
        {
            if (iar != null && iar.AsyncState != null)
            {
                ReceiveState receiveState = (ReceiveState) iar.AsyncState;
                try
                {
                    int received = receiveState.Cli.Socket.EndReceive(iar);
                    if (received != 0)
                    {
                        WebSocketFrame frame = WebSocketFrame.Parse(new NetworkStream(receiveState.Cli.Socket), receiveState.buffer);
                        if (frame != null)
                        {
							// Instead of trying to determine the WebSocketFrame subtype using the is or as keyword
							// we compare the OpCode ints directly to hopefully get more performance
							if (frame.OpCode == WebSocketOPCode.Continuation)
							{
								WebSocketContinuationFrame cont = (WebSocketContinuationFrame) frame;
								if (tl_frameBuffer.Value != null)
								{
									try
									{
										if (frame.Fin)
										{
											WebSocketMessage message = tl_frameBuffer.Value.End(cont);
											if (message.DataType == WebSocketDataType.Binary)
											{
												TextMessage txtMessage = (TextMessage) message;
												TextMessageEventArgs tmea = new TextMessageEventArgs(receiveState.Cli, txtMessage);
                                    			this.PushToEventQueue(tmea);
											}
											else
											{
												BinaryMessage binMessage = (BinaryMessage) message;
												BinaryMessageEventArgs bmea = new BinaryMessageEventArgs(receiveState.Cli, binMessage);
												this.PushToEventQueue(bmea);
											}

											tl_frameBuffer.Value = null;
										}
										else
										{
											tl_frameBuffer.Value.Append(cont);
											// TODO: Fire on continuation frame event
										}
									}
									catch (WebSocketServerException wssex)
									{
										tl_frameBuffer.Value = null;
										DisconnectionEventArgs de = new DisconnectionEventArgs(receiveState.Cli, wssex);
										this.PushToEventQueue(de);
                                    	this.RemoveClient(receiveState.Cli);
                                    	receiveState.Cli.Dispose();
									}
								}
								else
								{
									// TODO: Fail connection because the first frame of a fragmented message cannot have a Continuation OpCode
								}
							}
							else if (frame.OpCode == WebSocketOPCode.Text || frame.OpCode == WebSocketOPCode.Binary)
							{
                                WebSocketTextFrame textFrame = (WebSocketTextFrame) frame;
								if (!frame.Fin)
								{ // The message is fragmented
									if (tl_frameBuffer.Value == null)
									{
										this.tl_frameBuffer.Value = new WebSocketMessageBuffer(textFrame, 1024);
									}
									else
									{ // Received a frame with a non-continuation opcode before the end of another fragmented message
										//TODO: fail connection
									}
								}

                                if ( // Necessary to check for control characters because for an unknown reason some browsers seemed to send an empty text frame on disconnect
									!(String.IsNullOrEmpty(textFrame.Plaintext)
									|| (textFrame.Plaintext.Length == 1 && char.IsControl(textFrame.Plaintext.ElementAt(0))))
								)
                                {
									// TODO: Fire 'OnTextFrame' event
									TextMessage txtMessage = new TextMessage(textFrame.Plaintext);
									TextMessageEventArgs tmea = new TextMessageEventArgs(receiveState.Cli, txtMessage);
									PushToEventQueue(tmea);
                                    StartClientReceive(receiveState.Cli);
                                }
                                else
                                {
                                    // Many browsers and WebSocket client side implementations send an empty data frame on disconnection
                                    // for some obscure reason, so if it's the case, we disconnect the client 
                                    this.PushToEventQueue(new DisconnectionEventArgs(receiveState.Cli));
                                    this.RemoveClient(receiveState.Cli);
                                    receiveState.Cli.Dispose();
                                }
							}
                            else
                            {
                                this.DefaultControlFrameHandler(receiveState.Cli, (WebSocketControlFrame) frame);
                            }
                        }
                        else
                        {
                            this.RemoveClient(receiveState.Cli);
                            Exception e = new WebSocketServerException("Error while parsing an incoming WebSocketFrame");
                            this.PushToEventQueue(new DisconnectionEventArgs(receiveState.Cli, e));
                            receiveState.Cli.Dispose();
                        }
                    }
                    else
                    {
                        this.RemoveClient(receiveState.Cli);
                        this.PushToEventQueue(new DisconnectionEventArgs(receiveState.Cli));
                        receiveState.Cli.Dispose();
                    }
                }
                catch (Exception e)
                {
                    // TODO: Send a closing frame with 1011 "Internal Server Error" closing code as per protocol specifications
                    WebSocketCloseFrame cf = new WebSocketCloseFrame(WebSocketCloseCode.ProtocolError);
					this.SendControlFrame(receiveState.Cli, cf);
					this.RemoveClient(receiveState.Cli);
                    Exception ex = new WebSocketServerException("An error occured while processing message received from client. See inner exception for additional information", e);
                    this.PushToEventQueue(new DisconnectionEventArgs(receiveState.Cli, ex));
                    receiveState.Cli.Dispose();
                }
            }
        }

        /// <summary>
        /// Negotiates a WebSocket connection incoming from an HTTP connection
        /// </summary>
        /// <param name="s">Negotiation state object</param>
        private void NegotiateWebSocketConnection(Object? s)
        {
            if (s != null)
            {
                SocketNegotiationState state = (SocketNegotiationState) s;
                if (state.done == null)
                {
                    state.exception = new WebSocketNegotiationException("Negotiation state 'done' attribute is required but was nul");
                    return;
                }

                bool incomingOK = false;
                // Try to acquire a lock on an object used to parse the HTTP request to ensure only
                // one request is parsed at a time as for now, the parsing logic is by no mean thread safe
                // and the current method is executed by multiple ThreadPool threads at once
                // TODO: Make parsing logic thread safe so it can be executed in parallel; using a dedicated parser class could make sense in this context
                lock (this.headersmap)
                {
                    incomingOK = this.ReadRequestHeaders(state.handler) &&
                                this.AnalyzeRequestHeaders() &&
                                this.Negociate101Upgrade(state.handler);

                    Dictionary<string, byte[]> incomingHeadersMap = new Dictionary<string, byte[]>(this.headersmap);

                    if (incomingOK)
                    {
                        if (this.ClientInitializationStrategy != null)
                        {
                            EnjentHTTPRequest initialWSReq = new EnjentHTTPRequest(this.CurrentUrl, EnjentHTTPMethod.GET, incomingHeadersMap, this.QueryString);
                            TWebSocketClient cli = this.ClientInitializationStrategy(state.handler, initialWSReq);
                            state.cli = cli;
                            state.done(cli);
                        }
                        else
                        {
                            state.exception = new WebSocketNegotiationException("You are using a generic version of the WebSocketServer class but you did not specify a ClientInitializationStrategy");
                            state.done(null);
                        }
                    }
                    else
                    {
                        state.exception = new WebSocketNegotiationException("WebSocket negotiation failed");
                        state.done(null);
                    }
                }
            }
        }
    }
}
