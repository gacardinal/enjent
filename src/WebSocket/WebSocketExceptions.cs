using System;

namespace NarcityMedia.Enjent
{
	/// <summary>
	/// Represents errors relative to the WebSocket protocol.
	/// See the RFC6455 specification for informations relative to the WebSocket protocol
	/// </summary>
	class EnjentWebSocketException : Exception
	{
		public EnjentWebSocketException() : base() {}

		public EnjentWebSocketException(string message) : base(message) {}
		
		public EnjentWebSocketException(string message, Exception innerException) : base(message, innerException) {}
	}
}
