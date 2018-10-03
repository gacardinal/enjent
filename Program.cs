using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;


namespace dotnet_core_socket_server
{

    class State {
        private int bufferLength;
        private int writeindex;
        private static int BUFFER_LENGTH = 2048;
        public byte[] fulldata;
        private Socket handler;
        public State(int bufferLength, Socket handler) {
            this.bufferLength = bufferLength;
            this.writeindex = 0;
            this.fulldata = new byte[State.BUFFER_LENGTH];
            this.handler = handler;
        }

        public bool pushdata(byte[] buffer, int byteRead) {
            if (this.writeindex + byteRead <= State.BUFFER_LENGTH) {
                for (int i = 0; i < byteRead; i++) {
                    this.fulldata[i + this.writeindex] = buffer[i];
                }

                this.writeindex += byteRead;

                return true;
            } 
                
            return false;            
        }

        public int end(IAsyncResult ar) {
            return this.handler.EndReceive(ar);
        }
    }
    class Program
    {
        private const byte NEWLINE_BYTE = (byte)'\n'; 

        public static ManualResetEvent manualResetEvent = new ManualResetEvent(false);  
        public static List<Socket> Connections = new List<Socket>();


        public static void DidAcceptSocketConnection(IAsyncResult ar) {
            Console.WriteLine("Accepted connection");
            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            Connections.Add(handler);

            State state = new State(200, handler);
            byte[] buffer = new byte[200];
            int byteRead = 0;

            do {
                byteRead = handler.Receive(buffer);
                state.pushdata(buffer, byteRead);
            } while( buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );

            Console.WriteLine("Did read data from socket");

            Console.WriteLine(System.Text.Encoding.Default.GetString(state.fulldata));

            handler.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 200 OK\n\nHello, World!"));
            handler.Close();
            handler.Dispose();

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
