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
        private static Mutex mutex = new Mutex();

        private static List<Socket> socketList = new List<Socket>(2000);

        public static void DidAcceptSocketConnection(IAsyncResult ar) {
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ThreadPool.QueueUserWorkItem(NegotiateSocketConnection, handler);
            
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
                manualResetEvent.Reset();
                
                socket.BeginAccept(new AsyncCallback(DidAcceptSocketConnection), socket);

                manualResetEvent.WaitOne();
            }

            StopHttpServer();
        }

        private static void NegotiateSocketConnection(Object s)
        {
            Socket handler = (Socket) s;
            ClientObject cli = new ClientObject((Socket) handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
                {
                    Logger.Log("Acquiring mutex", Logger.LogType.Info);
                    if (mutex.WaitOne(5000))
                    {
                        cli.Greet();
                        Logger.Log("Socket connection accepted", Logger.LogType.Success);
                        Console.WriteLine("ORIGINAL COUNT: " + socketList.Count);
                        // TODO: Add socket to SocketManager
                        // TODO: Dispose of client object on disconnection
                        socketList.Add(handler);
                        Console.WriteLine("NEW COUNT: " + socketList.Count);
                        Logger.Log("Releasing mutex", Logger.LogType.Info);
                        mutex.ReleaseMutex();
                    }
                    else
                    {
                        Logger.Log("Main thread was not able to acquire the mutex to push a new socket to the local socket list", Logger.LogType.Error);
                    }

            } else {
                Logger.Log("Socket connection refused, couldn't parse headers", Logger.LogType.Error);
                cli.Dispose();
            }
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
