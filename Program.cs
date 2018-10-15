﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using NarcityMedia;
using NarcityMedia.Net;
using NarcityMedia.Log;

namespace dotnet_core_socket_server
{
    class Program
    {
        public static List<ClientObject> Connections = new List<ClientObject>();
        private static Boolean exit = false;
        private static Mutex mutex = new Mutex();
        private static List<Socket> socketList = new List<Socket>(2000);
        private const int MUTEX_TIMEOUT_DELAY = 5000;
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
                Socket handler = socket.Accept();
                DidAcceptSocketConnection(handler);
            }
        }
        
        public static void DidAcceptSocketConnection(Socket handler) {
            ThreadPool.QueueUserWorkItem(NegotiateSocketConnection, handler);
        }

        private static void NegotiateSocketConnection(Object s)
        {
            Socket handler = (Socket) s;
            ClientObject cli = new ClientObject((Socket) handler);
            if (cli.ReadRequestHeaders() &&
                cli.AnalyzeRequestHeaders() &&
                cli.Negociate101Upgrade() )
            {
                if (mutex.WaitOne(MUTEX_TIMEOUT_DELAY))
                {
                    cli.Greet();
                    Logger.Log("Socket connection accepted", Logger.LogType.Success);
                    Console.WriteLine("ORIGINAL COUNT: " + socketList.Count);
                    // TODO: Add socket to SocketManager
                    // TODO: Dispose of client object on disconnection
                    socketList.Add(handler);
                    Console.WriteLine("NEW COUNT: " + socketList.Count);
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
            HTTPServer httpServer = new HTTPServer("http://localhost:8887/");

            HTTPServer.EndpointCallback hello = Hello;
            HTTPServer.EndpointCallback sendNotificationToUser = SendNotificationToUser;
            HTTPServer.EndpointCallback sendnotificationToEndpoint = SendNotificationToEndpoint;

            httpServer.Get("/hello", hello);

            httpServer.Start();
        }

        private static void Hello(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to GET /hello", Logger.LogType.Success);
        }

        private static void SendNotificationToUser(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to POST /sendNotificationToUser", Logger.LogType.Success);
        }

        private static void SendNotificationToEndpoint(HttpListenerRequest req, HttpListenerResponse res)
        {
            Logger.Log("HTTP Request to POST /sendNotificationToEndpoint", Logger.LogType.Success);
        }
    }
}
