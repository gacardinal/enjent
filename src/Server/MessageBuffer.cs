using System;

using NarcityMedia.Enjent.WebSocket;

namespace NarcityMedia.Enjent.Server
{
	internal class WebSocketMessageBuffer
	{
		public readonly int MaxBufferSize;

		private WebSocketFrame[] frames;

		public int Count { get; private set; }

		public WebSocketMessageBuffer(int maxBufferSize) {
			this.MaxBufferSize = maxBufferSize;
			this.frames = new WebSocketFrame[maxBufferSize];
			this.Count = 0;
		}

		public bool Append(WebSocketFrame frame)
		{
			if (this.Count < this.MaxBufferSize) {
				this.frames[this.Count] = frame;
				this.Count++;
				return true;
			}

			return false;
		}

		public void Clear()
		{

		}
	}
}
