using System;

namespace NarcityMedia.Enjent.WebSocket
{
	/// <summary>
	/// Represents errors relative to the WebSocket protocol.
	/// See the RFC6455 specification for informations relative to the WebSocket protocol
	/// </summary>
	class EnjentWebSocketProtocolException : Exception
	{
		public EnjentWebSocketProtocolException() : base() {}

		public EnjentWebSocketProtocolException(string message) : base(message) {}
		
		public EnjentWebSocketProtocolException(string message, Exception innerException) : base(message, innerException) {}
	}
}
