using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using NarcityMedia;

namespace dotnet_core_socket_server
{
    class Program
    {
        public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);  
        public static List<ClientObject> Connections = new List<ClientObject>();

        private static Boolean exit = false;
        private static HttpListener HTTPServer = new HttpListener();

        public static void DidAcceptSocketConnection(IAsyncResult ar) {
            Console.WriteLine("Accepted connection");
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ClientObject cli = new ClientObject(handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
                {
                    cli.Greet();
                    // TODO: Add socket to SocketManager
                    // TODO: Dispose of client object on disconnection
            } else {
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
            
            socket.Bind(endpoint);
            socket.Listen(200);

            Console.WriteLine("Listening to connections");
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

            Console.WriteLine("HTTP Server is starting");

            while (HTTPServer.IsListening)
            {
                IAsyncResult result = HTTPServer.BeginGetContext(new AsyncCallback(Program.HTTPCallback), HTTPServer);
                result.AsyncWaitHandle.WaitOne();
                Console.WriteLine("Request processed asyncronously.");
            }
        }

        private static void HTTPCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;

            // Call EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            Console.WriteLine(request.Url);
            Console.WriteLine(request.HttpMethod);

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
