using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace NarcityMedia.Net
{
    public partial class WebSocketServer<TWebSocketClient> {

        /// <summary>
        /// Size of the chunks that will be read in RAM from the incoming socket connection
        /// </summary>
        private const int HEADER_CHUNK_BUFFER_SIZE = 1024 * 2;

        /// <summary>
        /// The website URL from which the new connection was initiated
        /// </summary>
        private string currentUrl;

        /// <summary>
        /// Byte representation of the 'new line' character
        /// </summary>
        private const byte NEWLINE_BYTE = (byte)'\n';

        /// <summary>
        /// Byte representation of the 'question amrk' character
        /// </summary>
        private const byte QUESTION_MARK_BYTE = (byte)'?';

        /// <summary>
        /// Byte representation of the 'colon' character
        /// </summary>
        private const byte COLON_BYTE = (byte)':';

        /// <summary>
        /// Byte representation of the 'space' character
        /// </summary>
        private const byte SPACE_BYTE = (byte)' ';

        /// <summary>
        /// The maximum accepted header size for the initial HTTP request
        /// </summary>
        private const int MAX_REQUEST_HEADERS_LENGTH = 2048;

        /// <summary>
        /// Sec-WebSocket-Key HTTP header name
        /// </summary>
        private const string WEBSOCKET_SEC_KEY_HEADER = "Sec-WebSocket-Key";

        /// <summary>
        /// Cookie HTTP header name
        /// </summary>
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

        /// <summary>
        /// HTTP method and path of the incoming HTTP request
        /// </summary>
        private byte[] methodandpath;

        /// <summary>
        /// Holds the headers of the incoming HTTP request
        /// </summary>
        private byte[] requestheaders = new byte[WebSocketServer.MAX_REQUEST_HEADERS_LENGTH];

        /// <summary>
        /// Associative mapping of the headers of the incoming HTTP request
        /// </summary>
        /// <typeparam name="string">Name of the HTTP header</typeparam>
        /// <typeparam name="byte[]">Byte representation of the HTTP header value</typeparam>
        /// <returns>Associative mapping of the incoming HTTP request's headers</returns>
        private Dictionary<string, byte[]> headersmap = new Dictionary<string, byte[]>();

        /// <summary>
        /// Length of the incoming HTTP request's headers
        /// </summary>
        private int requestheaderslength;

        /// <summary>
        /// Represents the index at which bytes read form the incomming socket should be written in <see cref="this.requestheaders" />
        /// </summary>
        private int writeindex = 0;

        /// <summary>
        /// Appends a chunk of bytes read from the incoming HTTP request to a buffer
        /// </summary>
        /// <param name="buffer">Buffer holding the chunk to append</param>
        /// <param name="byteRead">The number of bytes read</param>
        /// <returns>False if the number of bytes read exceeds the buffer size, true otherwise.</returns>
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

        /// <summary>
        /// Reads an incoming HTTP request from a given Socket
        /// </summary>
        /// <param name="socket">Socket from which to read the incoming HTTP request</param>
        /// <returns>False if the request exceeded the maximum length allowed, true otherwise</returns>
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

        /// <summary>
        /// Analyses the incoming HTTP request to ensure it is proprely formatted for a WebSocket upgrade
        /// </summary>
        /// <param name="socket">The socket from which to read the incoming HTTP request</param>
        /// <returns>Boolean value indicating whether the request was well formatted</returns>
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
                    byte[] bytesURL = new byte[i - urlStartImdex];
                    Array.Copy(this.requestheaders, urlStartImdex, bytesURL, 0, i - urlStartImdex);
                    this.currentUrl = System.Text.Encoding.Default.GetString(bytesURL);
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

        /// <summary>
        /// Prepares and sends an HTTP 101 upgrade response to finish the WebSocket handshake
        /// </summary>
        /// <param name="socket">The socket on which to send the 101 Upgrade</param>
        /// <returns>Boolean value indicating whether the upgraded was done successfully</returns>
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
