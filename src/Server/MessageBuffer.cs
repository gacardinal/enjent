using System;
using System.Linq;

using NarcityMedia.Enjent.WebSocket;

namespace NarcityMedia.Enjent.Server
{
	/// <summary>
	/// Acts as a buffer to hold a fragmented websocket message's fragments until the message is coplete
	/// </summary>
	internal class WebSocketMessageBuffer
	{
		/// <summary>
		/// The maximum number of fragments that are allowed in the current <see href="WebSocketMessageBuffer" /> instance
		/// </summary>
		public readonly uint MaxBufferSize;

		/// <summary>
		/// The internal fragment buffer
		/// </summary>
		private WebSocketFrame[] frames;

		/// <summary>
		/// The current number of elements that are held in the current <see href="WebSocketMessageBuffer" /> instance
		/// </summary>
		/// <value>Gets the current number of fragments held in the current buffer</value>
		public uint Count { get; private set; }

		/// <summary>
		/// The data type of the message being buffered by the current <see href="WebSocketMessageBuffer" /> instance
		/// </summary>
		/// <remarks>THe value of this field is determined by the first frame of the fragmented message</remarks>
		/// <value>Gets the </value>
		public WebSocketDataType MessageDataType { get; private set; }

		/// <summary>
		/// Initializes a new instance of <see cref="WebSocketMessageBuffer" />
		/// </summary>
		/// <param name="initialFrame">The first frame of the fragmented message. Determines the value of <see cref="MessageDataType" /></param>
		/// <param name="maxBufferSize">The maximum number of fragments a buffered message can contain</param>
		public WebSocketMessageBuffer(WebSocketDataFrame initialFrame, uint maxBufferSize)
		{
			if (initialFrame == null)
				throw new ArgumentNullException(nameof(initialFrame), "The initial frame cannot be null");
			if (maxBufferSize <= 0)
				throw new ArgumentException("The maxBufferSize must be greater than zero", nameof(maxBufferSize));

			this.MaxBufferSize = maxBufferSize;
			this.frames = new WebSocketDataFrame[maxBufferSize];
			this.frames[0] = initialFrame;
			this.MessageDataType = initialFrame.DataType;
			this.Count = 1;
		}

		/// <summary>
		/// Appends a new message fragment to the message buffer
		/// </summary>
		/// <param name="frame">The new message fragment</param>
		/// <exception cref="WebSocketServerException">If the buffer has reached its max count. See <see cref="MaxBufferSize" />.</exception>
		public void Append(WebSocketFrame frame)
		{
			if (this.Count < this.MaxBufferSize)
			{
				this.frames[this.Count] = frame;
				this.Count++;
			}
			else
			{
				throw new WebSocketServerException("Fragmented message exceeded the frame buffer max count");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="finalFrame"></param>
		/// <returns></returns>
		/// <exception cref="WebSocketServerException">If the buffer has reached its max count. See <see cref="MaxBufferSize" />.</exception>
		public WebSocketMessage End(WebSocketContinuationFrame finalFrame)
		{
			this.Append(finalFrame);

			int totalPayloadSize = this.frames.Sum(f => f.Payload.Length);
			byte[] wholePayload = new byte[totalPayloadSize];

			WebSocketMessage message;
			if (this.MessageDataType == WebSocketDataType.Text)
			{
				message = new TextMessage(wholePayload);
			}
			else
			{
				message = new BinaryMessage(wholePayload);
			}

			return message;
		}
	}
}
