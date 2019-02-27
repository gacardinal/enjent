using System;
using Xunit;
using System.Net;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
    public class WebSocketServer_Tests
    {
        IPEndPoint endpoint;
        WebSocketServer server;

        public WebSocketServer_Tests()
        {
            this.endpoint = new IPEndPoint(IPAddress.Loopback, 13003);
            this.server = new WebSocketServer();
        }

        [Fact]
        public void WebSocketServer_Start_Success_Path()
        {
            this.server.Start(this.endpoint);
        }

        [Fact]
        public void WebSocketServer_Start_Enpoint_Bound_Throw()
        {
            // Start once
            this.server.Start(this.endpoint);

            // Attempting to start again on an already bound endpoint should throw
            Assert.Throws<WebSocketServerException>(() => { this.server.Start(this.endpoint); });

            this.server.Stop();
        }
    }
}
