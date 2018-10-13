using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using NarcityMedia.Net;
using System.Runtime.InteropServices;

namespace NarcityMedia
{
    public enum HTTPMethods {
        GET, POST, DELETE, PUT
    }

    public class ClientObject : IDisposable {
        public bool Authenticated
        {
            get { return String.IsNullOrEmpty(this.lmlTk); }
        }

        private string lmlTk;
        private const byte NEWLINE_BYTE = (byte)'\n';
        private const byte QUESTION_MARK_BYTE = (byte)'?';
        private const byte COLON_BYTE = (byte)':';
        private const byte SPACE_BYTE = (byte)' ';
        private const int HEADER_CHUNK_BUFFER_SIZE = 1024;
        private const int MAX_REQUEST_HEADERS_LENGTH = 2048;
        private const string WEBSOCKET_SEC_KEY_HEADER = "Sec-WebSocket-Key";
        private const string WEBSOCKET_COOKIE_HEADER = "Cookie";
        private static readonly byte[] RFC6455_CONCAT_GUID = new byte[] {
            50, 53, 56, 69, 65, 70, 
            65, 53, 45, 69, 57, 49, 
            52, 45, 52, 55, 68, 65, 
            45, 57, 53, 67, 65, 45, 
            67, 53, 65, 66, 48, 68, 
            67, 56, 53, 66, 49, 49 
        };
        private static readonly byte[] GREET_MESSAGE = System.Text.Encoding.Default.GetBytes("Hello!");

        private Socket socket;
        private byte[] methodandpath;
        private byte[] requestheaders = new byte[ClientObject.MAX_REQUEST_HEADERS_LENGTH];
        private Dictionary<string, byte[]> headersmap = new Dictionary<string, byte[]>();
        
        private int requestheaderslength;
        private int writeindex = 0;
    
        public byte[] url;

        public ClientObject(Socket socket) {
            this.socket = socket;
        }

        protected void WriteBytes(byte[] message)
        {
            Console.WriteLine(System.Text.Encoding.Default.GetString(message));
        }

        private bool AppendHeaderChunk(byte[] buffer, int byteRead) {
            if (this.writeindex + byteRead <= ClientObject.HEADER_CHUNK_BUFFER_SIZE) {
                for (int i = 0; i < byteRead; i++) {
                    this.requestheaders[i + this.writeindex] = buffer[i];
                }

                this.writeindex += byteRead;
                this.requestheaderslength += byteRead;

                return true;
            } 
                
            return false;  
        }

        public bool ReadRequestHeaders() {
            byte[] buffer = new byte[HEADER_CHUNK_BUFFER_SIZE];
            int byteRead = 0;

            do {
                byteRead = this.socket.Receive(buffer);
                if (!this.AppendHeaderChunk(buffer, byteRead)) {
                    return false;
                }
            } while( byteRead != 0 && buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );
        
            return true;
        }

        public bool AnalyzeRequestHeaders() {
            bool lookingForQuestionMark = true;
            bool buildingQueryString = false;
            int urlStartImdex = 0;
            // First, read until new line to get method and path
            for (int i = 0; i < this.requestheaderslength; i++) {
                if (this.requestheaders[i] == ClientObject.NEWLINE_BYTE) {
                    this.methodandpath = new byte[i];
                    Array.Copy(this.requestheaders, 0, this.methodandpath, 0, i);                    

                    break;
                }
                else if (lookingForQuestionMark && this.requestheaders[i] == ClientObject.QUESTION_MARK_BYTE)
                {
                    urlStartImdex = i + 1; // +1 to ignore the '?'
                    lookingForQuestionMark = false;
                    buildingQueryString = true;
                }
                else if (buildingQueryString && this.requestheaders[i] == ClientObject.SPACE_BYTE)
                {
                    this.url = new byte[i - urlStartImdex];
                    Array.Copy(this.requestheaders, urlStartImdex, this.url, 0, i - urlStartImdex);
                    buildingQueryString = false;
                }
            }
            
            // If no '? was found request is invalid
            if (lookingForQuestionMark)
            {
                this.socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 401\n"));
                return false;
            }

            // Read until ":", then read until "new line"
            int lastStop = this.methodandpath.Length + 1;
            byte lookingFor = ClientObject.COLON_BYTE;
            bool trimming = true;
            bool lookingForColon = true;
            string currentheader = String.Empty;
            for (int i = lastStop; i < this.requestheaderslength; i++) {
                if (trimming && this.requestheaders[i] == ClientObject.SPACE_BYTE) {
                    lastStop++;
                    continue;
                } else {
                    trimming = false;
                }

                if (this.requestheaders[i] == lookingFor) {
                    int len = i - lastStop;
                    byte[] subset = new byte[len];
                    Array.Copy(this.requestheaders, lastStop, subset, 0, len);
                    
                    if (lookingForColon) {
                        currentheader = System.Text.Encoding.Default.GetString(subset);
                    } else {
                        this.headersmap[currentheader] = subset;
                    }

                    lookingFor = lookingForColon ? NEWLINE_BYTE : COLON_BYTE;
                    lookingForColon = !lookingForColon;
                    lastStop = i + 1;
                    trimming = true;
                }
            }

            return true;
        }

        public bool Negociate101Upgrade() {
            headersmap.Add("Cookie", System.Text.Encoding.Default.GetBytes("OGPC=19007661-2:; SID=jAY6bKZTa9dcTAHs9LnTVQBTT-4QSEUSdFuDfOYzSEyB2knFOqWfJWi2EC1FJVvdfvoTRQ.; APISID=_n81SRFO3YiOKNfu/AOxeOEisrh-E_t4Ig; SAPISID=_7nBHoE-frB6HRCQ/AclcTTOYUX6sISQny; 1P_JAR=2018-10-12-19; SIDCC=AGIhQKSTg1yFQOQXeo_F1cCHX1_MSgrFQQ3y4KXQZ-U_fMUk7lSfMG-ZJADSHhuC7D7EMVZwFEmt; lmltk=chlibadou"));
            Console.WriteLine(System.Text.Encoding.Default.GetString(this.requestheaders));
            if (this.headersmap.ContainsKey(ClientObject.WEBSOCKET_COOKIE_HEADER)) {
                String cookieData = System.Text.Encoding.Default.GetString(this.headersmap[ClientObject.WEBSOCKET_COOKIE_HEADER]);
                string cookieName = "lmltk=";
                int index = cookieData.IndexOf(cookieName);
                if (index != -1)
                {
                    int nextColon = cookieData.IndexOf(';', index + cookieName.Length);
                    string lmlTk = (nextColon != -1) ? cookieData.Substring(index + cookieName.Length, nextColon - index + cookieName.Length) 
                                                    : cookieData.Substring(index + cookieName.Length);
                    
                    this.lmlTk = lmlTk;
                    Console.WriteLine("LMLTK: " + lmlTk);
                }
            }

            if (this.headersmap.ContainsKey(ClientObject.WEBSOCKET_SEC_KEY_HEADER)) {
                byte[] seckeyheader = this.headersmap[ClientObject.WEBSOCKET_SEC_KEY_HEADER];
                byte[] tohash = new byte[seckeyheader.Length + ClientObject.RFC6455_CONCAT_GUID.Length - 1];
                for (int i = 0; i < seckeyheader.Length - 1; i++) {
                    tohash[i] = seckeyheader[i];
                }
                for (int i = 0; i < ClientObject.RFC6455_CONCAT_GUID.Length; i++) {
                    tohash[i + seckeyheader.Length - 1] = ClientObject.RFC6455_CONCAT_GUID[i];
                }

                string negociatedkey;
                using (SHA1Managed sha1 = new SHA1Managed()) {
                    var hash = sha1.ComputeHash(tohash);
                    negociatedkey = Convert.ToBase64String(hash);
                }

                this.socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 101 Switching Protocols\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Connection: upgrade\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Upgrade: websocket\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Extensions: permessage-deflate\n"));
                this.socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Accept: " + negociatedkey + "\n\n"));
            
                return true;
            } 

            return false;
        }

        public void Greet() {
            Console.WriteLine("Greeting new client");
            SocketMessage message = new SocketMessage(SocketMessage.ApplicationMessageCode.FetchComments);
            List<SocketFrame> frames = message.GetFrames();

            this.socket.Send(frames[0].GetBytes());
            // this.socket.Send(new byte[] { 
            //     0b10000001, (byte)(ClientObject.GREET_MESSAGE.Length) /*, 0b00000000, 0b00000000,
            //     0b00000000, 0b00000000, 0b00000000, 0b00000000, 
            //     0b00000000, 0b00000000, 0b00000000, 0b00000000, 
            //     0b00000000, 0b00000000,*/ 
            // });

            // this.socket.Send(ClientObject.GREET_MESSAGE);
        }

        public void Dispose() {
            if (this.socket != null) {
                this.socket.Dispose();
            }
        }
    }
}
