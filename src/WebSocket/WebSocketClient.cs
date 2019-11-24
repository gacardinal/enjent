using System;
using System.Net.Sockets;
using System.Collections.Generic;

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

        /// <summary>
        /// Unique identifier for the client
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// Represents the initial HTTP request that was used to negotiate the WebSocket connection
        /// </summary>
        public EnjentHTTPRequest? InitialRequest;

        /// <summary>
        /// The TCP socket associated with the current WebSocketClient
        /// </summary>
        /// <remarks>
        /// This field is reserved for internal use
        /// </remarks>
        internal Socket Socket;

        /// <summary>
        /// Initializes a new instance of a WebSocketClient
        /// </summary>
        /// <param name="socket">A connected TCP socket that represents a conneciton to a remote client</param>
        public WebSocketClient(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException("socket");

            this.Socket = socket;
            this.Id = Guid.NewGuid();
            this.InitTime = DateTime.Now;
        }

        public void Dispose()
        {
            if (this.Socket != null)
            {
                // Close method does Dispose of the object
                this.Socket.Close();
            }
        }

        /// <summary>
        /// Tries to send a WebSocketMessage to the socket associated with the current client
        /// </summary>
        /// <param name="message">The socket message to send</param>
        public bool Send(WebSocketMessage message)
        {
            foreach (WebSocketFrame frame in message.GetFrames())
            {
                
            }
            return true;
        }
    
        /// <summary>
        /// Sends a WebSocket control frame such as a 'pong' or a 'close' frame
        /// </summary>
        public bool SendControlFrame(WebSocketFrame frame)
        {
            List<WebSocketFrame> frames = new List<WebSocketFrame>(1) {frame};

            try
            {
                this.SendFrames(frames);
            }
            catch (Exception e)
            {
                // TODO Make it so it is the server who actually sends bytes trough the socket to allow better error handling
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends a text message to the socket associated with the current WebSocketClient
        /// </summary>
        /// <param name="message">The message to send</param>
        public bool Send(string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
            WebSocketDataFrame frame = new WebSocketDataFrame(true, false, bytes, WebSocketDataFrame.DataFrameType.Text);

            List<WebSocketFrame> frames = new List<WebSocketFrame>();
            frames.Add(frame);

            try
            {
                this.SendFrames(frames);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Sends Websocket frames via the client socket
        /// </summary>
        /// <param name="frames">A collection of WebSocket frames to send</param>
        /// <returns>A boolean value that indicates whether the send was successful or not</returns>
        /// <exception cref="SSystem.ArgumentNullException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.ObjectDisposedException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <remark>
        /// Any Exception raised by the socket.Send() method are meant to be caught by callers of this method
        /// </remark>
        protected void SendFrames(IEnumerable<WebSocketFrame> frames)
        {
            foreach (WebSocketFrame frame in frames)
            {
                this.Socket.Send(frame.GetBytes());
            }
        }
    }
}
