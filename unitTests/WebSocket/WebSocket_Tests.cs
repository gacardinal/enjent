using System;
using System.Diagnostics;
using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;
using System.Net;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{

    /// <summary>
    /// Tests features specific to the WebSocket protocol.
    /// Test data for this test class is defined in WebSocket_Tests_data.cs
    /// </summary>
    public partial class WebSocket_Tests : EnjentTest
    {
        private static Random rand = new Random();

		public WebSocket_Tests(ITestOutputHelper o) : base(o)
		{}
    }
}
