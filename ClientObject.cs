using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using NarcityMedia.Net;
using NarcityMedia.Log;
using System.Runtime.InteropServices;

namespace NarcityMedia
{
    public enum HTTPMethods {
        GET, POST, DELETE, PUT
    }

    class ClientObject : IDisposable {
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
            } while ( byteRead != 0 && buffer[byteRead - 1] != NEWLINE_BYTE && buffer[byteRead - 2] != NEWLINE_BYTE );
        
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

        /// <summary>
        /// Greets the client by sending the SocketMessage.ApplicationMessageCode.Greeting code.
        /// </summary>
        /// <remarks>Calls <see cref="SendApplicationMessage" /></remarks>
        public void Greet() {
            this.SendApplicationMessage(SocketMessage.ApplicationMessageCode.Greeting);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="message">The socket message to send</param>
        public void SendApplicationMessage(SocketMessage message)
        {
            List<SocketFrame> frames = message.GetFrames();
            try
            {
                this.socket.Send(frames[0].GetBytes());
            }
            catch (ArgumentNullException e)
            {
                Logger.Log("Attempted to send null value to socket - " + e.Message, Logger.LogType.Error);
            }
            catch (SocketException e)
            {
                Logger.Log("A SocketException occured, this could be due to a network problem or to the socket being closed by the OS - " + e.Message, Logger.LogType.Error);
                Logger.Log("SocketException.ErrorCode: " + e.ErrorCode, Logger.LogType.Error);
                Logger.Log("For SocketException error codes see https://bit.ly/2OsLovc", Logger.LogType.Info);
                Logger.Log("The closed socket will be disposed of", Logger.LogType.Info);
            }
            catch (ObjectDisposedException e)
            {
                Logger.Log("Message couldn't be sent, the socket was likely closed before attempting to send the message" + e.Message, Logger.LogType.Error);
            }
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="messageCode">The application message code to send</param>
        /// <remarks>Calls <see cref="SendApplicationMessage" /></remarks>
        public void SendApplicationMessage(SocketMessage.ApplicationMessageCode messageCode)
        {
            SocketMessage message = new SocketMessage(messageCode);
            this.SendApplicationMessage(message);
        }

        public void Dispose() {
            if (this.socket != null) {
                this.socket.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Represents an application message that is to be sent via WebSocket.
    /// A message is composed of frames
    /// </summary>
    /// <remarks>This class only support messages that can fit in a single frame for now</remarks>
    class SocketMessage
    {
        // Start values at value 1 to avoid sending empty application data
        public enum ApplicationMessageCode { Greeting = 1, FetchNOtifications, FetchCurrentArticle, FetchComments }
        public SocketDataFrame.DataFrameType MessageType = SocketDataFrame.DataFrameType.Binary;

        public ushort AppMessageCode
        {
            get { return this.appMessageCode; }
            set {
                this.appMessageCode = value;
                this.contentLength = MinimumPayloadSize();
            }
        }

        // WS standard allows 7 bits to represent message length in bytes
        private byte contentLength;
        private ushort appMessageCode;

        public SocketMessage(ApplicationMessageCode code)
        {
            this.AppMessageCode = (ushort) code;
        }

        /// <summary>
        /// Returns the Websocket frames that compose the current message, as per
        /// the websocket standard
        /// </summary>
        /// <remarks>The method currently supports only 1 frame messages</remarks>
        /// <returns>A List containing the frames of the message</returns>
        public List<SocketFrame> GetFrames()
        {
            byte[] payload = BitConverter.GetBytes((int)this.appMessageCode);
            List<SocketFrame> frames = new List<SocketFrame>(1);
            frames.Add(new SocketDataFrame(true, false, this.contentLength, this.MessageType, payload));

            return frames;
        }

        /// <summary>
        /// Returns a byte representing the minimum number of bytes needed to represent the AppMessageCode
        /// </summry>
        private byte MinimumPayloadSize()
        {
            byte minSize = 1;
            if ((ushort) this.appMessageCode >= 256) minSize = 2;

            return minSize;
        }
    }
}
