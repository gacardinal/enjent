using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace NarcityMedia.Enjent.WebSocket
{
	public interface IWebSocketMessage
	{

	}

	public abstract class WebSocketMessage<TFrameType> : IWebSocketMessage where TFrameType : WebSocketDataFrame, new()
	{
		public WebSocketDataType DataType;

		public byte[] Payload;

		public WebSocketMessage() : this(new byte[0])
		{}

		public WebSocketMessage(byte[] payload)
		{
			this.Payload = payload;
		}

        /// <summary>
        /// Returns the Websocket frames that compose the current message, as per
        /// the websocket standard
        /// </summary>
        /// <remarks>The method currently supports only 1 frame messages</remarks>
        /// <returns>A List containing the frames of the message</returns>
        public IEnumerable<TFrameType> GetFrames(bool masked = false)
        {
            TFrameType[] frames = new TFrameType[1];
			
            TFrameType frame = new TFrameType();
			frame.Fin = true;
			frame.Masked = masked;
			frame.Payload = this.Payload;

			frames[0] = frame;

            return frames;
        }
	}
	
    /// <summary>
    /// Represents a message that is to be sent via WebSocket.
    /// A message is composed of one or more frames.
    /// </summary>
    /// <remarks>This public class only support messages that can fit in a single frame for now</remarks>
    public class BinaryMessage : WebSocketMessage<WebSocketBinaryFrame>
    {
		/// <summary>
		/// Creates an instance of WebSocketMessage that contains the given payload.
		/// <see cref="MessageType" /> will be set to <see cref="TF.DataFrameType.Binary" />
		/// </summary>
		/// <param name="payload">The payload to initialize the message with</param>
        public BinaryMessage(byte[] payload) : base(payload)
        {
			this.DataType = WebSocketDataType.Binary;
		}
    }

	public class TextMessage : WebSocketMessage<WebSocketTextFrame>
	{
		private string _message;

		public string Message
		{
			get { return _message; }
			set
			{
				this._message = value;
				this.Payload = Encoding.UTF8.GetBytes(value);
			}
		}

		public TextMessage(byte[] bytes) : base(bytes)
		{
			this._message = Encoding.UTF8.GetString(bytes);
		}

		public TextMessage(string message)
		{
			this._message = message;
			this.Payload = Encoding.UTF8.GetBytes(message);
		}

		public TextMessage(WebSocketTextFrame[] frames)
		{
			this._message = string.Join(String.Empty, frames.Select(f => f.Plaintext));
		}
	}
}
