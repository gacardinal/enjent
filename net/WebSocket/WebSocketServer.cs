using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer<TWebSocketClient> where TWebSocketClient : WebSocketClient
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
        /// Internal Room that holds every client that is connected to the server
        /// </summary>
        private WebSocketRoom _allClients;

        /// <summary>
        /// Returns a <see cref="WebSocketRoom" /> that contains all the clients currently connected
        /// to the server
        /// </summary>
        /// <value>The returned <see cref="WebSocketRoom" /> is a copy of an internal WebsocketRoom object</value>
        public WebSocketRoom AllClients
        {
            get { return new WebSocketRoom(this._allClients); }
        }

        private List<WebSocketClient> clients;

        /// <summary>
        /// Instantiates a new instance of the WebSocketServer class
        /// </summary>
        public WebSocketServer()
        {
            this.listener = new Thread(this.NegociationLoop);
            // Set Thread as foreground to prevent program execution finishing
            this.listener.IsBackground = false;
            this.listener.Name = "WebSocketServerHTTPListener";

            this._allClients = new WebSocketRoom();
            this._allClients.Name = "GLOBAL";

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.clients = new List<WebSocketClient>(1024);
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
                    this.socket.Listen(1024);
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
        
        /// <summary>
        /// Executed by the 'listener' Thread, used to perform cleanup operation before quitting
        /// </summary>
        private void Quit()
        {

        }

        public void Stop()
        {
            this.listening = false;  // Listener Thread will exit when safe to do so
        }

        private void DefaultControlFrameHandler(WebSocketClient cli, SocketControlFrame cFrame)
        {
            switch (cFrame.opcode)
            {
                case 0: // Continuation
                    break;
                case 8: // Close
                    try
                    {
                        cli.SendControlFrame(new SocketControlFrame(true, false, SocketFrame.OPCodes.Close));
                        this.OnDisconnect.Invoke(this, new WebSocketServerEventArgs(cli, cFrame));
                    }
                    catch (Exception e)
                    {
                        WebSocketServerException ex = new WebSocketServerException("Error while sending 'close' control frame", e);
                        this.OnDisconnect.Invoke(this, new WebSocketServerEventArgs(cli, ex));
                    }
                    finally
                    {
                        cli.Dispose();
                    }
                    break;
                case 9: // Ping
                    try
                    {
                        cli.SendControlFrame(new SocketControlFrame(true, false, SocketFrame.OPCodes.Pong));
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
                        this.AddClient(cli);
                        this.OnConnect.Invoke(this, new WebSocketServerEventArgs(state.cli));
                    }
                };

                // 'WebSocketServerHTTPListener' threaad can move on and accept other requests
                ThreadPool.QueueUserWorkItem(WebSocketServer<TWebSocketClient>.NegociateWebSocketConnection, state);
            }

            this.Quit();
            return;     // End 'listener' Thread execution
        }

        private void AddClient(WebSocketClient cli)
        {
            lock (this.clients)
            {
                this.clients.Add(cli);
            }

            this.StartClientReceive(cli);
        }

        private void RemoveCLient(WebSocketClient cli)
        {
            if (cli != null)
            {
                lock (this.clients)
                {
                    this.clients.Remove(cli);
                }
            }
        }

        private void StartClientReceive(WebSocketClient cli)
        {
            ReceiveState receiveState = new ReceiveState();
            receiveState.Cli = cli;
            receiveState.Cli.socket.BeginReceive(receiveState.buffer, 0, ReceiveState.INIT_BUFFER_SIZE, 0,
                                    new AsyncCallback(ReceiveCallback), receiveState);
        }

        private void ReceiveCallback(IAsyncResult iar)
        {
            ReceiveState receiveState = (ReceiveState) iar.AsyncState;
            try
            {
                int received = receiveState.Cli.socket.EndReceive(iar);
                if (received != 0)
                {
                    SocketFrame frame = SocketFrame.TryParse(receiveState.buffer, receiveState.Cli.socket);
                    if (frame != null)
                    {
                        if (frame is SocketDataFrame)
                            this.OnMessage.Invoke(this, new WebSocketServerEventArgs(receiveState.Cli, (SocketDataFrame) frame));
                        else
                            this.DefaultControlFrameHandler(receiveState.Cli, (SocketControlFrame) frame);

                        StartClientReceive(receiveState.Cli);
                    }
                    else
                    {
                        this.RemoveCLient(receiveState.Cli);
                        Exception e = new WebSocketServerException("Error while parsing an incoming WebSocketFrame");
                        this.OnDisconnect.Invoke(this, new WebSocketServerEventArgs(receiveState.Cli, e));
                        receiveState.Cli.Dispose();
                    }
                }
                else
                {
                    this.RemoveCLient(receiveState.Cli);
                    this.OnDisconnect.Invoke(this, new WebSocketServerEventArgs(receiveState.Cli));
                    receiveState.Cli.Dispose();                    
                }
            }
            catch (Exception e)
            {
                this.RemoveCLient(receiveState.Cli);
                Exception ex = new WebSocketServerException("An error occured while processing message received from client. See inner exception for additional information", e);
                this.OnDisconnect.Invoke(this, new WebSocketServerEventArgs(receiveState.Cli, ex));
                receiveState.Cli.Dispose();
            }
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
}
