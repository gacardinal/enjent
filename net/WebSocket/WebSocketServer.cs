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
        private List<WebSocketPool> socketPools;
        private bool listening;
        private const int INITIAL_POOL_COUNT = 2;
        public WebSocketServer()
        {
            this.listener = new Thread(this.NegociationLoop);
            this.listener.IsBackground = false;
            this.listener.Name = "WebSocketServerHTTPListener";
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socketPools = new List<WebSocketPool>(INITIAL_POOL_COUNT);
            for (int i = 0; i < INITIAL_POOL_COUNT; i++)
            {
                this.socketPools.Add(new WebSocketPool());
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
                ThreadPool.QueueUserWorkItem(WebSocketServer.NegociateWebSocketConnection, handler);
            }

            this.Quit();
            return;     // End 'listener' Thread execution
        }

        public static void NegociateWebSocketConnection(Object s)
        {
            Socket handler = (Socket) s;
            WebSocketClient cli = new WebSocketClient((Socket) handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
            {
                cli.Greet();
                cli.StartListenAsync();
                // if (!SocketManager.Instance.AddClient(cli))
                // {
                //     cli.SendControlFrame(new SocketControlFrame(SocketFrame.OPCodes.Close));
                //     cli.Dispose();
                // }
            } else {
                cli.Dispose();
            }
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
    }

    internal class WebSocketPool
    {
        private Thread worker;
        private const int POOL_SIZE = 1024;
        public WebSocketPool()
        {
            this.worker = new Thread(ListenLoop);
            // Make thread foreground to prevent program execution ending
            this.worker.IsBackground = false;
        }

        private void ListenLoop()
        {
            while (true)
            {

            }
        }
    }
}
