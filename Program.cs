using System;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using NarcityMedia;
using NarcityMedia.Net;
using NarcityMedia.Log;
using Newtonsoft.Json;
using System.Linq;

namespace dotnet_core_socket_server
{
    public class Program
    {
        private static Boolean exit = false;
        private static HTTPServer httpServer;
        const string NAVIGATE_MARKER = "navigate:";

        static void Main(string[] args)
        {
            Thread HTTP = new Thread(Program.DispatchHTTPServer);
            HTTP.Start();

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 13003);

            try {
                socket.Bind(endpoint);
                socket.Listen(200);
                Logger.Log(String.Format("Listening for Web Socket connections at endpoint {0}:{1}", endpoint.Address, endpoint.Port), Logger.LogType.Success);
            } catch (SocketException) {
                Logger.Log(String.Format("Couldn't bind on endpoint {0}:{1} the port might already be in use", endpoint.Address, endpoint.Port),
                            Logger.LogType.Error);
                Environment.Exit(1);
            } catch {
                Logger.Log("Unknown error while trying to bind socket", Logger.LogType.Error);
                Environment.Exit(1);
            }

            int workerThreads, portThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out portThreads);
            Logger.Log("The process Thread Pool has a mixaimum of " + workerThreads.ToString() + " worker threads", Logger.LogType.Info);
            while (!Program.exit) {
                // MAIN THREAD blocks here and resumes on each new Socket connection
                Socket handler = socket.Accept();
                ThreadPool.QueueUserWorkItem(NegotiateSocketConnection, handler);
            }
        }

        private static void NegotiateSocketConnection(Object s)
        {
            Socket handler = (Socket) s;
            ClientObject cli = new ClientObject((Socket) handler);
            cli.OnMessage = OnSocketMessage;
            cli.OnClose = OnSocketClose;
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
            {
                cli.Greet();
                cli.StartListenAsync();
                Logger.Log("Socket connection accepted", Logger.LogType.Success);
                if (!SocketManager.Instance.AddClient(cli))
                {
                    Logger.Log("The new client couldn't be added to the Socket Manager and will be disposed of", Logger.LogType.Error);
                    cli.SendControlFrame(new SocketControlFrame(SocketFrame.OPCodes.Close));
                    cli.Dispose();
                }
            } else {
                Logger.Log("Socket connection refused, couldn't parse headers", Logger.LogType.Error);
                cli.Dispose();
            }
        }

        private static void OnSocketMessage(ClientObject client, SocketDataFrame message)
        {
            if (message.Plaintext.StartsWith(NAVIGATE_MARKER))
            {
                string newUrl = message.Plaintext.Substring(NAVIGATE_MARKER.Length);
                SocketManager.Instance.RemoveClient(client);
                client.currentUrl = newUrl;
                SocketManager.Instance.AddClient(client);
            }
            Logger.Log("Received message : " + message.Plaintext, Logger.LogType.Info);
        }

        private static void OnSocketClose(ClientObject client)
        {
            Logger.Log("Socket removed : " + SocketManager.Instance.RemoveClient(client), Logger.LogType.Info);
            // SocketManager.Instance.RemoveClient(client);
            double socketDuration = Math.Round(DateTime.Now.Subtract(client.InitTime).TotalSeconds, 2);
            Logger.Log(String.Format("Socket is closing after {0} seconds", socketDuration), Logger.LogType.Info);
            client.Dispose();
        }

        private static void DispatchHTTPServer()
        {
            httpServer = new HTTPServer(new Uri("http://localhost:8887"));

            // httpServer.on404 = on404;
            // httpServer.on500 = on500;

            HTTPServer.EndpointCallback index = Index;
            HTTPServer.EndpointCallback hello = Hello;
            HTTPServer.EndpointCallback stats = GetStats;
            HTTPServer.EndpointCallback sendNotificationToUser = SendNotificationToUser;
            HTTPServer.EndpointCallback sendnotificationToEndpoint = SendNotificationToEndpoint;

            httpServer.Get("/", index);
            httpServer.Get("/hello", hello);
            httpServer.Get("/stats", stats);
            httpServer.Get("/notifyuser", SendNotificationToUser);
            httpServer.Get("/notifyendpoint", SendNotificationToEndpoint);

            // .Start() is blocking meaning the thread won't finish until the server stops listenning
            httpServer.Start();
        }

        private static void Index(HttpListenerRequest req, HttpListenerResponse res)
        {
            httpServer.SendResponse(res, HttpStatusCode.OK, "GET Index");
        }

        private static void GetStats(HttpListenerRequest req, HttpListenerResponse res)
        {
            List<ClientObject> clients = SocketManager.Instance.GetClients();
            List<Room> rooms = SocketManager.Instance.GetRooms();
            int connections = clients.Count;

            // Process currentProcess = Process.GetCurrentProcess();
            // long ramBytesUsed = currentProcess.PagedMemorySize64;

            Stats stats = new Stats(rooms, connections);
            string serialized = JsonConvert.SerializeObject(stats, Formatting.None);

            httpServer.SendJSON(res, 200, serialized);
        }

        private static void Hello(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to GET /hello", Logger.LogType.Success);
            httpServer.SendResponse(res, HttpStatusCode.OK, "GET /hello");
        }

        private static void SendNotificationToUser(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to POST /sendNotificationToUser", Logger.LogType.Success);
            httpServer.SendResponse(res, HttpStatusCode.OK, "GET /notifyuser");
        }

        private static void SendNotificationToEndpoint(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to POST /sendNotificationToEndpoint", Logger.LogType.Success);
            // Hardcoded for testing purposes
            Room endpointRoom = SocketManager.Instance.GetRoomByName("www.test.narcity.com/test");
            if (endpointRoom != null)
            {
                endpointRoom.Broadcast(WebSocketMessage.ApplicationMessageCode.FetchComments);
                httpServer.SendResponse(res, HttpStatusCode.OK, "GET /notifyendpoint");
            }
            else
            {
                httpServer.SendResponse(res, HttpStatusCode.NotFound, "Couldn't find any room with the specified endpoint");
            }
        }
        
        private static void on404(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log(String.Format("HTTP 404 - Couldn't find endpoint {0}", req.Url.ToString()), Logger.LogType.Warning);
            httpServer.SendResponse(res, HttpStatusCode.NotFound, "Not Found");
        }

        private static void on500(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP 500 - Internal Server Error", Logger.LogType.Error);
            httpServer.SendResponse(res, HttpStatusCode.NotFound, "Internal Server Error");
        }

        private class Stats
        {
            public int RealtimeSessionsNumber;
            public int ActiveEndpointsNumber;
            public List<Room> rooms;

            public Stats(List<Room> rooms)
            {
                this.rooms = rooms.OrderByDescending(room => room.Clients.Count).ToList();
                this.ActiveEndpointsNumber = rooms.Count;
            }

            public Stats(List<Room> rooms, int realtimeSessionsNumber) : this(rooms)
            {
                this.RealtimeSessionsNumber = realtimeSessionsNumber;
            }
        }
    }
}
