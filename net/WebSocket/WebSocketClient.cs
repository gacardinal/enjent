using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NarcityMedia.Net;
using NarcityMedia.Log;

namespace NarcityMedia.Net
{
    public enum HTTPMethods {
        GET, POST, DELETE, PUT
    }

    public partial class WebSocketClient : IDisposable {
        public bool Authenticated
        {
            get { return String.IsNullOrEmpty(this.lmlTk); }
        }

        /// <summary>
        /// Unix timestamp (32 bits unsigned) that represents the time at which the current object was created
        /// </summary>
        public DateTime InitTime = DateTime.Now;
        public string lmlTk;
        public delegate void SocketDataFrameHandler(WebSocketClient client, SocketDataFrame frame);
        public SocketDataFrameHandler OnMessage;
        public delegate void ClientEvent(WebSocketClient client);
        public ClientEvent OnClose;
        private delegate void SocketControlFrameHandler(SocketControlFrame frame);
        private SocketControlFrameHandler OnControlFrame;
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
        private byte[] requestheaders = new byte[WebSocketClient.MAX_REQUEST_HEADERS_LENGTH];
        private Dictionary<string, byte[]> headersmap = new Dictionary<string, byte[]>();
        private bool listenToSocket = true;
        private int requestheaderslength;
        private int writeindex = 0;

        private byte[] _url;
        private byte[] url
        {
            get { return this._url; }
            set{
                this._url = value;
                this.currentUrl = System.Text.Encoding.Default.GetString(value);
            }
        }

        public string currentUrl;

        public delegate void SocketMessageCallback(WebSocketMessage message);

        public WebSocketClient(Socket socket)
        {
            this.socket = socket;
            this.OnControlFrame = DefaultControlFrameHandler;
            this.OnMessage = DefaultDataFrameHandler;
            this.lmlTk = GenerateRandomToken(32, false);
        }

        /// <summary>
        /// Starts listenning to the WebSocket associated to the current client object on a separate thread
        /// </summary>
        public void StartListenAsync()
        {
            this.listener.Start(this.socket);
        }

        // See https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs?view=netframework-4.7.2
        protected void BeginListening(Object state) 
        {
            Socket socket = (Socket) state;
            byte[] frameHeaderBuffer = new byte[2];
            try
            {
                while (this.listenToSocket)
                {
                    int received = socket.Receive(frameHeaderBuffer); // Blocking
                    SocketFrame frame = this.TryParse(frameHeaderBuffer);
                    if (frame != null)
                    {
                        if (frame is SocketControlFrame)
                            this.OnControlFrame((SocketControlFrame) frame);
                        else if (frame is SocketDataFrame)
                            this.OnMessage(this, (SocketDataFrame) frame);
                    }
                    else
                    {
                        if (!this.SendControlFrame(new SocketControlFrame(true, false, SocketFrame.OPCodes.Close)))
                            this.listenToSocket = false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, Logger.LogType.Error);
                this.Dispose();
            }

            Logger.Log("Socket closing", Logger.LogType.Info);
            // End thread execution
            return;
        }

        private void DefaultControlFrameHandler(SocketControlFrame frame)
        {
            switch (frame.opcode)
            {
                case 0: // Continuation
                    this.listenToSocket = false;
                    break;
                case 8: // Close
                    this.SendControlFrame(new SocketControlFrame(true, false, SocketFrame.OPCodes.Close));
                    this.listenToSocket = false;
                    if (this.OnClose != null) this.OnClose(this);
                    break;
                case 9:
                    Logger.Log("Received ping", Logger.LogType.Info);
                    this.SendControlFrame(new SocketControlFrame(true, false, SocketFrame.OPCodes.Pong));
                    break;
                default:
                    this.listenToSocket = false;
                    break;
            }
        }

        private static void DefaultDataFrameHandler(WebSocketClient cli, SocketDataFrame frame)
        {
            cli.SendApplicationMessage(WebSocketMessage.ApplicationMessageCode.Greeting);
        }

        /// <summary>
        /// Greets the client by sending the SocketMessage.ApplicationMessageCode.Greeting code.
        /// </summary>
        /// <remarks>Calls <see cref="SendApplicationMessage" /></remarks>
        public void Greet()
        {
            this.SendApplicationMessage(WebSocketMessage.ApplicationMessageCode.Greeting);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="message">The socket message to send</param>
        public bool SendApplicationMessage(WebSocketMessage message)
        {
            List<SocketFrame> frames = message.GetFrames();
            return SendFrames(frames);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="messageCode">The application message code to send</param>
        /// <remarks>Calls <see cref="SendApplicationMessage" /></remarks>
        public bool SendApplicationMessage(WebSocketMessage.ApplicationMessageCode messageCode)
        {
            WebSocketMessage message = new WebSocketMessage(messageCode);
            return this.SendApplicationMessage(message);
        }
    
        /// <summary>
        /// Sends Websocket frames via the client socket
        /// </summary>
        /// <param name="frames">The list of frames to send</param>
        /// <returns>A boolean value that indicates whether the send was successful or not</returns>
        private bool SendFrames(List<SocketFrame> frames)
        {
            try
            {
                foreach (SocketFrame frame in frames)
                {
                    this.socket.Send(frame.GetBytes());
                }

                return true;
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

            return false;
        }

        /// <summary>
        /// Sends a websocket control frame such as a 'pong' or a 'close' frame
        /// </summary>
        /// <param name="frame">The control frame to send</param>
        public bool SendControlFrame(SocketFrame frame)
        {
            List<SocketFrame> frames = new List<SocketFrame>(1);
            frames.Add(frame);

            return this.SendFrames(frames);
        }

        
        /// <summary>
        /// Tries to parse a byte[] header into a SocketDataFrame object.
        /// Returns a null reference if object cannot be parsed
        /// </summary>
        /// <param name="headerBytes"></param>
        /// <returns>
        /// If pasrse is successful, an Object of a type that is derived from SocketFrame.abstract Returns a null pointer otherwise.
        /// </returns>
        /// <remarks>
        /// This method is not exactly like the Int*.TryParse() methods as it doesn't take an 'out' parameter and return a
        /// boolean value but rather returns either the parsed object reference or a null reference, which means that callers of this method need to check
        /// for null before using the return value.
        /// Furthermore, if the parse is successful, a caller should check the type of the object that is returned to, for example,
        /// differenciate between a SocketDataFrame and a SocketControlFrame, which are both derived from SocketFrame.
        /// </remarks>
        public SocketFrame TryParse(byte[] headerBytes)
        {
            int headerSize = headerBytes.Length;
            bool fin = (headerBytes[0] >> 7) != 0;
            byte opcode = (byte) (headerBytes[0] & 0b00001111);
            bool masked = (headerBytes[1] & 0b10000000) != 0;
            ushort contentLength = (ushort) (headerBytes[1] & 0b01111111);

            if (contentLength <= 126)
            {
                if (contentLength == 126)
                {
                    headerSize = 4;
                    byte[] largerHeader = new byte[headerSize];
                    headerBytes.CopyTo(largerHeader, 0);
                    // Read next two bytes and interpret them as the content length
                    this.socket.Receive(largerHeader, 2, 2, SocketFlags.None);
                    contentLength = (ushort) (largerHeader[2] << 8 | largerHeader[3]);
                }

                byte[] maskingKey = new byte[4];
                this.socket.Receive(maskingKey);

                byte[] contentBuffer = new byte[contentLength];
                if (contentLength > 0) this.socket.Receive(contentBuffer);

                SocketFrame frame;
                if (opcode == 1 || opcode == 2)
                    frame = new SocketDataFrame(fin, masked, contentLength, (SocketDataFrame.DataFrameType) opcode, UnmaskContent(contentBuffer, maskingKey));
                else
                    frame = new SocketControlFrame(fin, masked, (SocketFrame.OPCodes)opcode);

                return frame;
            }

            return null;
        }

        private static byte[] UnmaskContent(byte[] masked, byte[] maskingKey)
        {
            if (maskingKey.Length != 4) throw new ArgumentException("Masking key must always be of length 4");

            if (masked.Length == 0) return new byte[0];

            byte[] unmasked = new byte[masked.Length];

            for (int i = 0; i < masked.Length; i++)
            {
                unmasked[i] = (byte) (masked[i] ^ maskingKey[i % 4]);
            }

            return unmasked;
        }

        public void Dispose()
        {
            this.listenToSocket = false;
            if (this.socket != null) {
                this.socket.Dispose();
            }
        }
    }
    
    /// <summary>
    /// Represents an application message that is to be sent via WebSocket.
    /// A message is composed of frames
    /// </summary>
    /// <remarks>This public class only support messages that can fit in a single frame for now</remarks>
    public class WebSocketMessage
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

        public WebSocketMessage(ApplicationMessageCode code)
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
