using System;
using Xunit;
using Xunit.Abstractions;
using NarcityMedia.Enjent;

namespace EnjentUnitTests.WebSocket
{

	public partial class DataFrameTests : EnjentTest
	{
		public DataFrameTests(ITestOutputHelper o) : base(o)
		{}

        [Fact]
		public void WebSocketDataFrame_Has_Correct_DataType()
		{
			WebSocketTextFrame txtFrame = new WebSocketTextFrame("Some text");
			Assert.True(WebSocketDataType.Text == txtFrame.DataType, "WebSOcketDataFrame => TextFrame did not have correct datatype");

			WebSocketBinaryFrame binFrame = new WebSocketBinaryFrame(new byte[0]);
			Assert.True(WebSocketDataType.Binary == binFrame.DataType, "WebSOcketDataFrame => BinaryFrame did not have correct datatype");
		}
	}
}
