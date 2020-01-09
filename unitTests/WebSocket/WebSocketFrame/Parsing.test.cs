using System;
using Xunit;
using NarcityMedia.Enjent;

using NarcityMedia.Enjent.WebSocket;

namespace EnjentUnitTests.WebSocket
{
	public partial class WebSocketFrameTest
	{
        [Fact]
        public void WebSocketFrame_TryParse_Valid_DataFrame_Text_SingleFrame()
        {
            string testContent = "test!";

            bool fin = true;
            WebSocketOPCode OPCode = WebSocketOPCode.Text;
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(testContent);

            // Manually craft a WebSocketFrame
            byte[] maskingK = new byte[4];
            // The masking key is supposed to be cryptographically secure but this will suffice for testing purposes
            rand.NextBytes(maskingK);
        }
	}
}
