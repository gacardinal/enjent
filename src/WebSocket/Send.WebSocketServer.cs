using System;
using System.Collections.Generic;

namespace NarcityMedia.Enjent
{
    public partial class WebSocketServer<TWebSocketClient>
    {
        public void Send(WebSocketClient cli, string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
            WebSocketDataFrame frame = new WebSocketDataFrame(true, false, bytes, WebSocketDataFrame.DataFrameType.Text, bytes);

            List<WebSocketFrame> frames = new List<WebSocketFrame>();
            frames.Add(frame);

            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the current client
        /// </summary>
        /// <param name="message">The socket message to send</param>
        public void Send(WebSocketClient cli, WebSocketMessage message)
        {
            List<WebSocketFrame> frames = message.GetFrames();
            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends an application message to the socket associated with the specified client
        /// </summary>
        /// <param name="messageCode">The application message code to send</param>
        /// <remarks>Calls <see cref="Send" /></remarks>
        public void Send(WebSocketClient cli, WebSocketMessage.ApplicationMessageCode messageCode)
        {
            WebSocketMessage message = new WebSocketMessage(messageCode);            
            this.Send(cli, message);
        }

        /// <summary>
        /// Sends a websocket control frame such as a 'pong' or a 'close' frame to a specified client's socket
        /// </summary>
        /// <param name="cli">The client to send the frames to</param>
        /// <param name="frame">The frames to send to the client</param>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        public void SendControlFrame(WebSocketClient cli, WebSocketControlFrame frame)
        {
            List<WebSocketFrame> frames = new List<WebSocketFrame>(1) {frame};
            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends one or more WebSocketFrames to the specified client
        /// </summary>
        /// <param name="cli">The client to send the frames to</param>
        /// <param name="frames">The frames to send to the client</param>
        /// <exception cref="SSystem.ArgumentNullException"><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException"><exception/>
        /// <exception cref="SSystem.ObjectDisposedException"><exception/>
        internal void SendFrames(WebSocketClient cli, List<WebSocketFrame> frames)
        {
            foreach (WebSocketFrame frame in frames)
            {
                cli.socket.Send(frame.GetBytes());
            }
        }
    }
}
