using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace NarcityMedia.Net
{
    /// <summary>
    /// An instance of WebSocketPool, not to be confused with a Room, is used to regroup sockets together so a
    /// Thread can iterate over them and listen to them all.
    /// This avoids blocking the main WebSocket negotiation loop while avoiding to dispatch a Thread for every new WebSocket connection.
    /// </summary>
    internal class WebSocketPool
    {
        private Thread worker;
        public readonly int POOL_SIZE = 1024;
        public readonly int POOL_ID = 0;
        public List<WebSocketClient> clients { get; }

        public event FrameHandler OnPoolFrame;

        /// <summary>
        /// An event delegate that is raised by the WebSocketPool when it detects that one of its sockets received a WebSocket frame.
        /// This event does not implement the 'Microsoft .NET standard' f(object sender, EventArgs e) signature because this
        /// event is very custom to the WebSocketServer application and also, it is internal to the namespace.
        /// </summary>
        /// <param name="cli">The WebSocketCLient object that receives the frame</param>
        /// <param name="frame">The frame that was received by the WebSocketClient</param>
        public delegate void FrameHandler(WebSocketClient cli, SocketFrame frame);

        public WebSocketPool(int id)
        {
            this.POOL_ID = id;
            this.clients = new List<WebSocketClient>(this.POOL_SIZE);
            this.worker = new Thread(ListenLoop);
            this.worker.Name = "ThreadPoolWorker_" + this.POOL_ID++;
        }

        public WebSocketPool(int id, int poolSize) : this(id)
        {
            this.POOL_SIZE = poolSize;
        }

        public void StartListening()
        {
            this.worker.Start();
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
                                    if (this.OnPoolFrame != null)
                                    {
                                        this.OnPoolFrame.Invoke(cli, frame);
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
                                throw e;
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

                // Avoid capping CPU
                Thread.Sleep(1);
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

        public event FrameHandler OnManagerFrame;
        public delegate void FrameHandler(WebSocketClient cli, SocketFrame frame);

        public WebSocketPoolManager(FrameHandler f)
        {
            this.socketPools = new List<WebSocketPool>(INITIAL_POOL_COUNT);
            this.clientPoolsAssociations = new List<ClientPoolAssoc>(INITIAL_POOL_COUNT * 1024);
            this.OnManagerFrame += f;
            for (int i = 0; i < INITIAL_POOL_COUNT; i++)
            {
                WebSocketPool pool = new WebSocketPool(i);
                pool.OnPoolFrame += this.FrameHandlerCallback;
                this.socketPools.Add(pool);
                pool.StartListening();
            }
        }

        /// <summary>
        /// Callback that is executed when a WebSocketPool parses a WebSocketFrame comming from a client
        /// </summary>
        /// <param name="cli">The client that sent the frame</param>
        /// <param name="frame">The frame that was sent</param>
        /// <remarks>
        /// </remarks>
        private void FrameHandlerCallback(WebSocketClient cli, SocketFrame frame)
        {
            foreach (WebSocketPool pool in this.socketPools)
            {
                Console.WriteLine("Pool " + pool.POOL_ID + " has " + pool.clients.Count + " clients");
            }
            this.OnManagerFrame.Invoke(cli, frame);
        }

        /// <summary>
        /// Inserts a client object in a WebSocketPool's socket collection and always inserts the given
        /// client object in the pool that contains the less elements to keep the pools balanced
        /// </summary>
        /// <param name="cli">The client object to insert</param>
        public void AddClient(WebSocketClient cli)
        {
            WebSocketPool pool = this.socketPools.Aggregate((a, b) => a.clients.Count < b.clients.Count ? a : b);
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
