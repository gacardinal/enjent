using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace dotnet_core_socket_server
{
    public enum HTTPMethods {
        GET, POST, DELETE, PUT
    }

    public class ClientObject : IDisposable {
        private const byte NEWLINE_BYTE = (byte)'\n';
        private const byte COLON_BYTE = (byte)':';
        private const byte SPACE_BYTE = (byte)' ';
        private const int HEADER_CHUNK_BUFFER_SIZE = 1024;
        private const int MAX_REQUEST_HEADERS_LENGTH = 2048;
        private const string WEBSOCKET_SEC_KEY_HEADER = "Sec-WebSocket-Key";
        private static readonly byte[] RFC6455_CONCAT_GUID = new byte[] {
            50, 53, 56, 69, 65, 70, 65, 53, 45, 69, 57, 49, 
            52, 45, 52, 55, 68, 65, 45, 57, 53, 67, 65, 45, 
            67, 53, 65, 66, 48, 68, 67, 56, 53, 66, 49, 49 
        };

        private Socket socket;
        private byte[] methodandpath;
        private byte[] requestheaders = new byte[ClientObject.MAX_REQUEST_HEADERS_LENGTH];
        private Dictionary<string, byte[]> headersmap = new Dictionary<string, byte[]>();
        
        private int requestheaderslength;
        private int writeindex = 0;
    
        public ClientObject(Socket socket) {
            this.socket = socket;
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

        public void ReadRequestHeaders() {
            byte[] buffer = new byte[HEADER_CHUNK_BUFFER_SIZE];
            int byteRead = 0;

            do {
                byteRead = this.socket.Receive(buffer);
                this.AppendHeaderChunk(buffer, byteRead);
            } while( buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );
        }

        public void AnalyzeRequestHeaders() {
            // First, read until new line to get method and path
            for (int i = 0; i < this.requestheaderslength; i++) {
                if (this.requestheaders[i] == ClientObject.NEWLINE_BYTE) {
                    this.methodandpath = new byte[i];
                    Array.Copy(this.requestheaders, 0, this.methodandpath, 0, i);                    
                
                    break;
                }
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
        }

        public void Negociate101Upgrade() {
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
            } else {
                throw new Exception("No SEC key found.");
            }
        }

        public void Dispose() {
            if (this.socket != null) {
                this.socket.Dispose();
            }
        }
    }
}

/*

Websocket example to respond to :

GET /test HTTP/1.1
Host localhost:13003
Connection Upgrade
Pragma no-cache
Cache-Control no-cache
User-Agent Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.92 Safari/537.36
Upgrade websocket
Origin null
Sec-WebSocket-Version 13
Accept-Encoding gzip, deflate, br
Accept-Language en-US,en;q=0.9,en-GB;q=0.8,fr-CA;q=0.7,fr;q=0.6,ca;q=0.5
Cookie _ga=GA1.1.141756367.1538080381
Sec-WebSocket-Key e5OsiUCZmjzRPb66zBFBiA==
Sec-WebSocket-Extensions permessage-deflate; client_max_window_bits

 */