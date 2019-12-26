using System;
using System.Collections.Generic;

namespace NarcityMedia.Enjent
{
	public abstract class WebSocketMessage
	{
		public WebSocketDataType DataType;

		public abstract IEnumerable<WebSocketDataFrame> GetFrames();
	}
	
    /// <summary>
    /// Represents a message that is to be sent via WebSocket.
    /// A message is composed of one or more frames.
    /// </summary>
    /// <remarks>This public class only support messages that can fit in a single frame for now</remarks>
    public class BinaryMessage
    {
		/// <summary>
		/// Represents the type of data contained in the current message
		/// </summary>
		public readonly WebSocketDataType DataType = WebSocketDataType.Binary;

		/// <summary>
		/// Creates an instance of WebSocketMessage that contains the given payload.
		/// <see cref="MessageType" /> will be set to <see cref="WebSocketDataFrame.DataFrameType.Binary" />
		/// </summary>
		/// <param name="payload">The payload to initialize the message with</param>
        public BinaryMessage(byte[] payload)
        {
			this.DataType = WebSocketDataType.Binary;
		}

		/// <summary>
		/// Creates an instance of WebSocketMessage that contains the given message as payload.
		/// <see cref="MessageType" /> will be set to <see cref="WebSocketDataFrame.DataFrameType.Text" />
		/// </summary>
		/// <param name="message">The message to set as the payload</param>
		public BinaryMessage(string message)
		{
			this.DataType = WebSocketDataType.Text;
		}

        /// <summary>
        /// Returns the Websocket frames that compose the current message, as per
        /// the websocket standard
        /// </summary>
        /// <remarks>The method currently supports only 1 frame messages</remarks>
        /// <returns>A List containing the frames of the message</returns>
        public List<WebSocketBinaryFrame> GetFrames()
        {
            List<WebSocketBinaryFrame> frames = new List<WebSocketBinaryFrame>(1);
			
            frames.Add(new WebSocketBinaryFrame(true, false, this.Payload));

            return frames;
        }
    }
}
