using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NarcityMedia.Log;

namespace NarcityMedia.Enjent
{
    /// <summary>
    /// Represents a client a.k.a. an end user that connected to the server by the HTTP protocol
    /// and that has subsequently upgraded to the WebSocket protocol.
    /// </summary>
    public partial class WebSocketClient : IDisposable
    {
        /// <summary>
        /// Unix timestamp (32 bits unsigned) that represents the time at which the current object was created
        /// </summary>
        public readonly DateTime InitTime;
        public readonly Guid Id;
        public Socket socket;
        private byte[] _url;
        private byte[] url
        {
            get { return this._url; }
            set{
                this._url = value;
                this.currentUrl = System.Text.Encoding.Default.GetString(value);
            }
        }

        public EnjentHTTPRequest InitialRequest;

        public string currentUrl;

        public delegate void SocketMessageCallback(WebSocketMessage message);

        public WebSocketClient(Socket socket)
        {
            this.Id = Guid.NewGuid();
            this.InitTime = DateTime.Now;
            this.socket = socket;
        }

        public void Dispose()
        {
            if (this.socket != null) {
                // Close method does Dispose of the object
                this.socket.Close();
            }
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="message">The socket message to send</param>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        /// <remarks>
        /// Callers must call this method in a try statement because it will not catch exceptions
        /// raised by this.SendFrames()
        /// </remarks>
        public void Send(WebSocketMessage message)
        {
            List<WebSocketFrame> frames = message.GetFrames();
            SendFrames(frames);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="messageCode">The application message code to send</param>
        /// <remarks>Calls <see cref="Send" /></remarks>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        /// <remarks>
        /// Callers must call this method in a try statement because it will not catch exceptions
        /// raised by this.SendFrames()
        /// </remarks>
        public void Send(WebSocketMessage.ApplicationMessageCode messageCode)
        {
            WebSocketMessage message = new WebSocketMessage(messageCode);            
            this.Send(message);
        }
    
        /// <summary>
        /// Sends a websocket control frame such as a 'pong' or a 'close' frame
        /// </summary>
        /// <param name="frame">The control frame to send</param>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        /// <remarks>
        /// Callers must call this method in a try statement because it will not catch exceptions
        /// raised by this.SendFrames()
        /// </remarks>
        public void SendControlFrame(WebSocketFrame frame)
        {
            List<WebSocketFrame> frames = new List<WebSocketFrame>(1);
            frames.Add(frame);

            this.SendFrames(frames);
        }

        /// <summary>
        /// Sends a text message to the socket associated with the current WebSocketClient
        /// </summary>
        /// <param name="message">The message to send</param>
        public void Send(string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
            WebSocketDataFrame frame = new WebSocketDataFrame(true, false, (ushort) bytes.Length, WebSocketDataFrame.DataFrameType.Text, bytes);

            List<WebSocketFrame> frames = new List<WebSocketFrame>();
            frames.Add(frame);

            this.SendFrames(frames);
        }

        /// <summary>
        /// Sends Websocket frames via the client socket
        /// </summary>
        /// <param name="frames">The list of frames to send</param>
        /// <returns>A boolean value that indicates whether the send was successful or not</returns>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        /// <remark>
        /// Any Exception raised by the socket.Send() method are meant to be caught by callers of this method
        /// </remark>
        private void SendFrames(List<WebSocketFrame> frames)
        {
            foreach (WebSocketFrame frame in frames)
            {
                this.socket.Send(frame.GetBytes());
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
        public WebSocketDataFrame.DataFrameType MessageType = WebSocketDataFrame.DataFrameType.Binary;

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
        public List<WebSocketFrame> GetFrames()
        {
            byte[] payload = BitConverter.GetBytes((int)this.appMessageCode);
            List<WebSocketFrame> frames = new List<WebSocketFrame>(1);
            frames.Add(new WebSocketDataFrame(true, false, this.contentLength, this.MessageType, payload));

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
