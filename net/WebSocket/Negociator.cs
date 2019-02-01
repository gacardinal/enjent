using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer<TWebSocketClient> {
        private const int HEADER_CHUNK_BUFFER_SIZE = 1024 * 2;
        private const byte NEWLINE_BYTE = (byte)'\n';
        private const byte QUESTION_MARK_BYTE = (byte)'?';
        private const byte COLON_BYTE = (byte)':';
        private const byte SPACE_BYTE = (byte)' ';
        private const int MAX_REQUEST_HEADERS_LENGTH = 2048;
        private const string WEBSOCKET_SEC_KEY_HEADER = "Sec-WebSocket-Key";
        private const string WEBSOCKET_COOKIE_HEADER = "Cookie";
        /// <summary>
        /// Key defined in RFC 6455 necessary to the negociation of a ne WebSocket conneciton
        /// </summary>
        /// <value>Byte array representing the RFC 6455 key</value>
        private static readonly byte[] RFC6455_CONCAT_GUID = new byte[] {
            50, 53, 56, 69, 65, 70, 
            65, 53, 45, 69, 57, 49, 
            52, 45, 52, 55, 68, 65, 
            45, 57, 53, 67, 65, 45, 
            67, 53, 65, 66, 48, 68, 
            67, 56, 53, 66, 49, 49 
        };
        private byte[] methodandpath;
        private byte[] requestheaders = new byte[WebSocketServer.MAX_REQUEST_HEADERS_LENGTH];
        private Dictionary<string, byte[]> headersmap = new Dictionary<string, byte[]>();
        private int requestheaderslength;
        private int writeindex = 0;

        private bool AppendHeaderChunk(byte[] buffer, int byteRead)
        {
            if (this.writeindex + byteRead <= WebSocketServer.HEADER_CHUNK_BUFFER_SIZE)
            {
                for (int i = 0; i < byteRead; i++)
                {
                    this.requestheaders[i + this.writeindex] = buffer[i];
                }

                this.writeindex += byteRead;
                this.requestheaderslength += byteRead;

                return true;
            }
                
            return false;  
        }

        private bool ReadRequestHeaders(Socket socket)
        {
            byte[] buffer = new byte[HEADER_CHUNK_BUFFER_SIZE];
            int byteRead = 0;

            do
            {
                byteRead = socket.Receive(buffer);

                if (!this.AppendHeaderChunk(buffer, byteRead))
                {
                    return false;
                }
            }
            while ( byteRead != 0 && buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );

            return true;
        }

        private bool AnalyzeRequestHeaders(Socket socket)
        {
            bool lookingForQuestionMark = true;
            bool buildingQueryString = false;
            int urlStartImdex = 0;
            // First, read until new line to get method and path
            for (int i = 0; i < this.requestheaderslength; i++)
            {
                if (this.requestheaders[i] == WebSocketServer.NEWLINE_BYTE)
                {
                    this.methodandpath = new byte[i];
                    Array.Copy(this.requestheaders, 0, this.methodandpath, 0, i);                    

                    break;
                }
                else if (lookingForQuestionMark && this.requestheaders[i] == WebSocketServer.QUESTION_MARK_BYTE)
                {
                    urlStartImdex = i + 1; // +1 to ignore the '?'
                    lookingForQuestionMark = false;
                    buildingQueryString = true;
                }
                else if (buildingQueryString && this.requestheaders[i] == WebSocketServer.SPACE_BYTE)
                {
                    // this.url = new byte[i - urlStartImdex];
                    // Array.Copy(this.requestheaders, urlStartImdex, this.url, 0, i - urlStartImdex);
                    // this.currentUrl = System.Text.Encoding.Default.GetString(this.url);
                    buildingQueryString = false;
                }
            }
            
            // If no '? was found request is invalid
            if (lookingForQuestionMark)
            {
                socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 401\n"));
                return false;
            }

            // Read until ":", then read until "new line"
            int lastStop = this.methodandpath.Length + 1;
            byte lookingFor = WebSocketServer.COLON_BYTE;
            bool trimming = true;
            bool lookingForColon = true;
            string currentheader = String.Empty;
            for (int i = lastStop; i < this.requestheaderslength; i++)
            {
                if (trimming && this.requestheaders[i] == WebSocketServer.SPACE_BYTE)
                {
                    lastStop++;
                    continue;
                }
                else
                {
                    trimming = false;
                }

                if (this.requestheaders[i] == lookingFor)
                {
                    int len = i - lastStop;
                    byte[] subset = new byte[len];
                    Array.Copy(this.requestheaders, lastStop, subset, 0, len);
                    
                    if (lookingForColon)
                    {
                        currentheader = System.Text.Encoding.Default.GetString(subset);
                    }
                    else
                    {
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

        private bool Negociate101Upgrade(Socket socket)
        {
            if (this.headersmap.ContainsKey(WebSocketServer.WEBSOCKET_COOKIE_HEADER))
            {
                String cookieData = System.Text.Encoding.Default.GetString(this.headersmap[WebSocketServer.WEBSOCKET_COOKIE_HEADER]);
                string cookieName = "lmltk=";
                int index = cookieData.IndexOf(cookieName);
                if (index != -1)
                {
                    int nextColon = cookieData.IndexOf(';', index + cookieName.Length);
                    // string lmlTk = (nextColon != -1) ? cookieData.Substring(index + cookieName.Length, nextColon - index + cookieName.Length) 
                    //                                 : cookieData.Substring(index + cookieName.Length);
                }
            }

            if (this.headersmap.ContainsKey(WebSocketServer.WEBSOCKET_SEC_KEY_HEADER))
            {
                byte[] seckeyheader = this.headersmap[WebSocketServer.WEBSOCKET_SEC_KEY_HEADER];
                byte[] tohash = new byte[seckeyheader.Length + WebSocketServer.RFC6455_CONCAT_GUID.Length - 1];
                for (int i = 0; i < seckeyheader.Length - 1; i++)
                {
                    tohash[i] = seckeyheader[i];
                }
                for (int i = 0; i < WebSocketServer.RFC6455_CONCAT_GUID.Length; i++) 
                {
                    tohash[i + seckeyheader.Length - 1] = WebSocketServer.RFC6455_CONCAT_GUID[i];
                }

                string negociatedkey;
                using (SHA1Managed sha1 = new SHA1Managed()) {
                    var hash = sha1.ComputeHash(tohash);
                    negociatedkey = Convert.ToBase64String(hash);
                }

                socket.Send(System.Text.Encoding.Default.GetBytes("HTTP/1.1 101 Switching Protocols\n"));
                socket.Send(System.Text.Encoding.Default.GetBytes("Connection: upgrade\n"));
                socket.Send(System.Text.Encoding.Default.GetBytes("Upgrade: websocket\n"));
                socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Accept: " + negociatedkey));

                if (this.headersmap.ContainsKey("Sec-WebSocket-Protocol"))
                {
                    byte[] protocol = new byte[0];
                    this.headersmap.TryGetValue("Sec-WebSocket-Protocol", out protocol);
                    socket.Send(System.Text.Encoding.Default.GetBytes("Sec-WebSocket-Protocol: " + protocol));
                }

                socket.Send(System.Text.Encoding.Default.GetBytes("\n\n"));
            
                return true;
            }

            return false;
        }
    }
}
