using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer
    {
        /// <summary>
        /// Thread that will listen for incoming connections
        /// </summary>
        private Thread listener;

        /// <summary>
        /// The socket that will be bound on the endpoint passed to the WebSocketServer.Start() method
        /// </summary>
        private Socket socket;

        /// <summary>
        /// Indicates whether the server is listening for incoming connections
        /// </summary>
        private bool listening;

        /// <summary>
        /// A WebSocketPoolManager instance to which all new WebSockets clients will be added
        /// </summary>
        private WebSocketPoolManager poolManager;
        
        /// <summary>
        /// Instantiates a new instance of the WebSocketServer class
        /// </summary>
        public WebSocketServer()
        {
            this.listener = new Thread(this.NegociationLoop);
            // Set Thread as foreground to prevent program execution finishing
            this.listener.IsBackground = false;
            this.listener.Name = "WebSocketServerHTTPListener";
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.poolManager = new WebSocketPoolManager();
            this.poolManager.OnFrame = this.FrameHandler;
        }

        /// <summary>
        /// Executed by the 'listener' Thread, used to perform cleanup operation before quitting
        /// </summary>
        private void Quit()
        {

        }

        private void FrameHandler(WebSocketClient cli, SocketFrame frame)
        {
            if (frame is SocketControlFrame)
                ;
            else if (frame is SocketDataFrame)
                ;
        }

        /// <summary>
        /// Starts listenning for incomming HTTP WebSocket connections
        /// </summary>
        /// <param name="endpoint">The endpoint at which the WebSocketServer should listen for incomming WebSocket HTTP connections</param>
        /// <exception cref="WebSocketServerException">Thros any exception that might occur while trying to bind the socket as an inner exception</exception>
        public void Start(IPEndPoint endpoint)
        {
            if (!this.listening)
            {
                try
                {
                    this.socket.Bind(endpoint);
                    this.socket.Listen(200);
                    this.listening = true;
                    this.listener.Start();
                }
                catch (SocketException e)
                {
                    throw new WebSocketServerException(String.Format("Couldn't bind on endpoint {0}:{1} the port might already be in use", endpoint.Address, endpoint.Port), e);
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
        }

        public void Stop()
        {
            this.listening = false;  // Listener Thread will exit when safe to do so
        }


        private class SocketNegotiationState
        {
            public Socket handler;
            public WebSocketClient cli;
            public WebSocketNegotiationException exception;
            public delegate void NegotiationCallback(WebSocketClient cli);
            public NegotiationCallback done;

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
            while (this.listening)
            {
                Socket handler = this.socket.Accept();  // Blocking
                SocketNegotiationState state = new SocketNegotiationState(handler);
                state.done = cli => {
                    // Executed async once the negotiation is done
                    if (state.exception == null)
                    {
                        this.poolManager.AddClient(cli);
                        this.OnConnect.Invoke(this, new WebSocketServerEventArgs(state.cli));
                    }
                };

                // 'WebSocketServerHTTPListener' threaad can move on and accept other requests
                ThreadPool.QueueUserWorkItem(WebSocketServer.NegociateWebSocketConnection, state);
            }

            this.Quit();
            return;     // End 'listener' Thread execution
        }

        public static void NegociateWebSocketConnection(Object s)
        {
            SocketNegotiationState state = (SocketNegotiationState) s;
            WebSocketClient cli = new WebSocketClient(state.handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
            {
                state.cli = cli;
                state.done(cli);
            } else {
                state.exception = new WebSocketNegotiationException("WebSocket negotiation failed");
                cli.Dispose();
            }
        }
    }

    /// <summary>
    /// An instance of WebSocketPool, not to be confused with a Room, is used to regroup sockets together so a
    /// Thread can iterate over them and listen to them all.
    /// This avoids blocking the main WebSocket negotiation loop while avoiding to dispatch a Thread for every new WebSocket connection.
    /// </summary>
    internal class WebSocketPool
    {
        private Thread worker;
        public readonly int POOL_SIZE = 1024;
        private static int POOL_ID = 0;
        public List<WebSocketClient> clients { get; }

        public FrameHandler OnFrame;
        public delegate void FrameHandler(WebSocketClient cli, SocketFrame frame);

        public WebSocketPool()
        {
            this.clients = new List<WebSocketClient>(this.POOL_SIZE);
            this.worker = new Thread(ListenLoop);
            this.worker.Name = "ThreadPoolWorker_" + WebSocketPool.POOL_ID++;
            this.worker.Start();
        }

        public WebSocketPool(int poolSize) : this()
        {
            this.POOL_SIZE = poolSize;
        }

        private void ListenLoop()
        {
            while (true)
            {
                lock (this.clients)
                {
                    foreach (WebSocketClient cli in this.clients)
                    {
                        // WebSocket frames are 2 bytes minimum
                        if (cli.socket.Available >= 2)
                        {
                            // The try / catch clause inside a for inside a while shouldn't affectr performances.
                            // The only performance hit should occur when an Exception is thrown by the listening logic but that's, well, exceptionnal
                            try
                            {
                                byte[] frameHeaderBuffer = new byte[2];
                                int received = cli.socket.Receive(frameHeaderBuffer); // Blocking
                                SocketFrame frame = SocketFrame.TryParse(frameHeaderBuffer, cli.socket);
                                if (frame != null)
                                {
                                    if (this.OnFrame != null)
                                    {
                                        this.OnFrame.Invoke(cli, frame);
                                    }
                                }
                                else
                                {
                                    // Parsing error
                                }
                            }
                            catch (Exception e)
                            {
                                // Move on with iterating over the other sockets to make sure none are left unattended
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a WebSocketClient to the current WebSocketPool
        /// </summary>
        /// <param name="cli">A reference to the client to add</param>
        /// <remarks>
        /// This method is to be executed by the same thread that executes the ThreadPoolManager logic
        /// </remarks>
        public void AddClient(WebSocketClient cli)
        {
            lock (this.clients)
            {
                this.clients.Add(cli);
            }
        }

        /// <summary>
        /// Removes a WebSocketClient from the current WebSocketPool
        /// </summary>
        /// <param name="cli">A reference to the client to remove</param>
        /// <returns>
        /// Returns a boolean that represents whether the operation was successful
        /// </returns>
        /// <remarks>
        /// This method is to be executed by the same thread that executes the ThreadPoolManager logic
        /// </remarks>
        public bool RemoveClient(WebSocketClient cli)
        {
            lock (this.clients)
            {
                return this.clients.Remove(cli);
            }
        }
    }

    /// <summary>
    /// Maintains a collection of WebSOcketPool objects.
    /// Each WebSocketPool within the WebSocketPoolManager gets assigned a Thread that will constantly
    /// check the buffer of each WebSocket within the WebSocketPool and takes actions according to the state of the buffer.
    /// The WebSocketPoolManager ensures that all WebSocketPools contain a balanced number of WebSockets to maintain
    /// consistent performances.
    /// </summary>
    internal class WebSocketPoolManager
    {
        private const int INITIAL_POOL_COUNT = 2;
        private List<WebSocketPool> socketPools;
        private List<ClientPoolAssoc> clientPoolsAssociations;

        public FrameHandler OnFrame;
        public delegate void FrameHandler(WebSocketClient cli, SocketFrame frame);

        public WebSocketPoolManager()
        {
            this.socketPools = new List<WebSocketPool>(INITIAL_POOL_COUNT);
            this.clientPoolsAssociations = new List<ClientPoolAssoc>(INITIAL_POOL_COUNT * 1024);
            for (int i = 0; i < INITIAL_POOL_COUNT; i++)
            {
                WebSocketPool pool = new WebSocketPool();
                pool.OnFrame = this.FrameHandlerCallback;
                this.socketPools.Add(new WebSocketPool());
            }
        }

        /// <summary>
        /// Callback that is executed when a WebSocketPool parses a WebSocketFrame comming from a client
        /// </summary>
        /// <param name="cli">The client that sent the frame</param>
        /// <param name="frame">The frame that was sent</param>
        /// <remarks>
        /// This method will invoke the meth
        /// </remarks>
        private void FrameHandlerCallback(WebSocketClient cli, SocketFrame frame)
        {
            if (this.OnFrame != null)
            {
                this.OnFrame.Invoke(cli, frame);
            }
        }

        /// <summary>
        /// Inserts a client object in a WebSocketPool's socket collection and always inserts the given
        /// client object in the pool that contains the less elements to keep the pools balanced
        /// </summary>
        /// <param name="cli">The client object to insert</param>
        public void AddClient(WebSocketClient cli)
        {
            WebSocketPool pool = this.socketPools.OrderByDescending(x => x.clients.Count).First();
            if (pool != null) {
                this.clientPoolsAssociations.Add(new ClientPoolAssoc(cli, pool));
                pool.AddClient(cli);
            }
        }

        /// <summary>
        /// Locates and removes a given client from its thread pool
        /// </summary>
        /// <param name="cli">The client object to remove</param>
        /// <returns>Whether the remove operation was successful</returns>
        public bool RemoveClient(WebSocketClient cli)
        {
            ClientPoolAssoc cliAssoc = this.clientPoolsAssociations.Find(x => x.client == cli);
            this.clientPoolsAssociations.Remove(cliAssoc);
            return cliAssoc.pool.RemoveClient(cli);
        }

        /// <summary>
        /// Used to associate a client object with the websocketpool it's in to avoid having to loop through
        /// all of the websocketpools to find or delete a client object
        /// </summary>
        private struct ClientPoolAssoc
        {
            public WebSocketClient client;
            public WebSocketPool pool;

            public ClientPoolAssoc(WebSocketClient client, WebSocketPool pool)
            {
                this.client = client;
                this.pool = pool;
            }
        }
    }
}
