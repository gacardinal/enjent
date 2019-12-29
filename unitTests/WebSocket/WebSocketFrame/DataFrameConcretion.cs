using System;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
	class WebSocketFrameConcretion : WebSocketFrame
	{
		public WebSocketFrameConcretion(bool fin, bool masked, byte[] payload) : base(fin, masked, payload)
		{}
	}
}
