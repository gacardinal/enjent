using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;


namespace dotnet_core_socket_server
{
    class Program
    {
        public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);  
        public static List<ClientObject> Connections = new List<ClientObject>();

        public static void DidAcceptSocketConnection(IAsyncResult ar) {
            Console.WriteLine("Accepted connection");
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ClientObject cli = new ClientObject(handler);
            cli.ReadRequestHeaders();
            cli.AnalyzeRequestHeaders();
            cli.Negociate101Upgrade();
            
            cli.Dispose();

            manualResetEvent.Set();
        }

        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 13003);
            
            socket.Bind(endpoint);
            socket.Listen(200);

            Console.WriteLine("Listening to connections");
            while (true) {
                manualResetEvent.Reset();
                
                socket.BeginAccept(new AsyncCallback(DidAcceptSocketConnection), socket);

                manualResetEvent.WaitOne();
            }
        }
    }
}
