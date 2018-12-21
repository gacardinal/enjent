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
        private Thread listener;
        private Socket socket;
        private bool listening;
        public WebSocketServer()
        {
            this.listener = new Thread(this.NegociationLoop);
            this.listener.IsBackground = false;
            this.listener.Name = "WebSocketServerHTTPListener";
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
    
        /// <summary>
        /// Executed by the 'listener' Thread, used to perform cleanup operation before quitting
        /// </summary>
        private void Quit()
        {

        }

        /// <summary>
        /// Starts listenning for incomming HTTP WebSocket connections
        /// </summary>
        /// <param name="endpoint">The endpoint at which the WebSocketServer should listen for incomming WebSocket HTTP connections</param>
        /// <exception cref="WebSocketServerException">Thros any exception that might occur while trying to bing the socket as an inner exception</exception>
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
                cli.StartListenAsync();
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

        public WebSocketPool()
        {
            this.clients = new List<WebSocketClient>(this.POOL_SIZE);
            
            this.worker = new Thread(ListenLoop);
            this.worker.Name = "ThreadPoolWorker_" + WebSocketPool.POOL_ID++;
            // Make thread foreground to prevent program execution ending
            this.worker.IsBackground = false;

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
                        
                    }
                }
            }
        }

        public void AddClient(WebSocketClient cli)
        {
            lock (this.clients)
            {
                this.clients.Add(cli);
            }
        }

        public bool RemoveCLient(WebSocketClient cli)
        {
            lock (this.clients)
            {
                this.clients.Remove(cli);
            }
        }
    }

    public class WebSocketPoolManager
    {
        private List<WebSocketPool> socketPools;
        private const int INITIAL_POOL_COUNT = 2;
        private List<ClientPoolAssoc> clientPoolsAssoc;

        public WebSocketPoolManager()
        {
            this.socketPools = new List<WebSocketPool>(INITIAL_POOL_COUNT);
            this.clientPoolsAssoc = new List<ClientPoolAssoc>(this.INITIAL_POOL_COUNT * 1024);
            for (int i = 0; i < INITIAL_POOL_COUNT; i++)
            {
                this.socketPools.Add(new WebSocketPool());
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
                this.clientPoolsAssoc.Add(new ClientPoolAssoc(cli, pool));
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
            ClientPoolAssoc cliAssoc = this.clientPoolsAssoc.Find(x => x.client == cli);
            this.clientPoolsAssoc.Remove(cliAssoc);
            return cliAssoc.pool.Remove(cli);
        }

        /// <summary>
        /// Used to associate a client object with the websocketpool it's in to avoid having to loop through
        /// all of the websocketpools to find or delete a client object
        /// </summary>
        private struct ClientPoolAssoc
        {
            public WebSocketClient client;
            public WebsSocketPool pool;

            public ClientPoolAssoc(WebSocketClient client, WebSocketPool pool)
            {
                this.client = client;
                this.pool = pool;
            }
        }
    }
}
