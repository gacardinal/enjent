using System;
using System.Collections.Generic;

namespace NarcityMedia.Enjent
{
    public abstract partial class WebSocketServerCore<TWebSocketClient> where TWebSocketClient : WebSocketClient
    {
        /// <summary>
        /// Sends a message to the socket associated with the current client
        /// </summary>
        /// <param name="cli">The client to send the message to</param>
        /// <param name="message">The socket message to send</param>
        /// <exception cref="SSystem.ArgumentNullException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.ObjectDisposedException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        public void Send(TWebSocketClient cli, string message)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
            WebSocketDataFrame frame = new WebSocketDataFrame(true, false, bytes, WebSocketDataType.Text);

            List<WebSocketFrame> frames = new List<WebSocketFrame>();
            frames.Add(frame);

            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends a WebSocketMessage to the socket associated with the given client
        /// </summary>
        /// <param name="cli">The client to send the message to</param>
        /// <param name="message">The socket message to send</param>
        /// <exception cref="SSystem.ArgumentNullException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.ObjectDisposedException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        public void Send(TWebSocketClient cli, BinaryMessage message)
        {
            IEnumerable<WebSocketFrame> frames = message.GetFrames();
            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends a websocket control frame such as a 'pong' or a 'close' frame to a specified client's socket
        /// </summary>
        /// <param name="cli">The client to send the frames to</param>
        /// <param name="frame">The frames to send to the client</param>
        /// <exception cref="SSystem.ArgumentNullException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.ObjectDisposedException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        public void SendControlFrame(TWebSocketClient cli, WebSocketControlFrame frame)
        {
            List<WebSocketFrame> frames = new List<WebSocketFrame>(1) {frame};
            this.SendFrames(cli, frames);
        }

        /// <summary>
        /// Sends a list of WebSocketFrames to the specified client
        /// </summary>
        /// <param name="cli">The client to send the frames to</param>
        /// <param name="frames">The frames to send to the client</param>
        /// <exception cref="SSystem.ArgumentNullException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.Net.Sockets.SocketException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        /// <exception cref="SSystem.ObjectDisposedException">See <see cref="System.Net.Sockets.Socket.Send(byte[])" /><exception/>
        internal void SendFrames(TWebSocketClient cli, IEnumerable<WebSocketFrame> frames)
        {
			try
			{
				foreach (WebSocketFrame frame in frames)
				{
					cli.Socket.Send(frame.GetBytes());
				}
			}
			catch (Exception e)
			{
				WebSocketServerException ex = new WebSocketServerException("Error while sending WebSocket message. Connection will be dropped forcefully. See inner exception for additional information.", e);
				this.PushToEventQueue(new DisconnectionEventArgs(cli, ex));
			}
        }
    }
}
