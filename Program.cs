using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using NarcityMedia;
using NarcityMedia.Log;

namespace dotnet_core_socket_server
{
    class Program
    {
        public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);  
        public static List<ClientObject> Connections = new List<ClientObject>();

        private static Boolean exit = false;
        private static HttpListener HTTPServer = new HttpListener();

        public static void DidAcceptSocketConnection(IAsyncResult ar) {
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ClientObject cli = new ClientObject(handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
                {
                    Logger.Log("Socket connection accepted", Logger.LogType.Success);
                    cli.Greet();
                    // TODO: Add socket to SocketManager
                    // TODO: Dispose of client object on disconnection
            } else {
                Logger.Log("Socket connection refused, couldn't parse headers", Logger.LogType.Error);
                cli.Dispose();
            }
            
            manualResetEvent.Set();
        }

        static void Main(string[] args)
        {
            Thread HTTP = new Thread(Program.DispatchHTTPServer);
            HTTP.Start();

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 13003);
            
            try {
                socket.Bind(endpoint);
                socket.Listen(200);
            } catch (SocketException) {
                Logger.Log(String.Format("Couldn't bind on endpoint {0}:{1} the port might already be in use", endpoint.Address.ToString(), endpoint.Port.ToString()),
                            Logger.LogType.Error);
                Environment.Exit(1);
            } catch {
                Logger.Log("Unknown error while trying to bind socket", Logger.LogType.Error);
                Environment.Exit(1);
            }

            Logger.Log("Listening for socket connections", Logger.LogType.Info);
            while (!Program.exit) {
                manualResetEvent.Reset();
                
                socket.BeginAccept(new AsyncCallback(DidAcceptSocketConnection), socket);

                manualResetEvent.WaitOne();
            }

            // Program is stopping
            StopHttpServer();
        }

        private static void DispatchHTTPServer()
        {
            HTTPServer.Prefixes.Add("http://localhost:8887/");
            HTTPServer.Start();

            Logger.Log("HTTP Server is starting", Logger.LogType.Info);

            while (HTTPServer.IsListening)
            {
                IAsyncResult result = HTTPServer.BeginGetContext(new AsyncCallback(Program.HTTPCallback), HTTPServer);
                result.AsyncWaitHandle.WaitOne();
            }
        }

        private static void HTTPCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;

            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            response.AddHeader("Content-Type", "text/plain");
            string responseText = "ALLO";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer,0,buffer.Length);
            // You must close the output stream.
            output.Close();
        }

        private static void StopHttpServer()
        {
            HTTPServer.Close();
        }
    }
}
