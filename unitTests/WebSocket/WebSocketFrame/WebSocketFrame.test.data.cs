using Xunit;

using NarcityMedia.Enjent.WebSocket;

namespace EnjentUnitTests.WebSocket
{
    public partial class WebSocketFrameTest : EnjentTest
    {
		public static TheoryData<WebSocketFrame> GetBytes_WebSocketFrame_Subtypes
		{
			get
			{
				TheoryData<WebSocketFrame> data = new TheoryData<WebSocketFrame>();

				data.Add(new WebSocketBinaryFrame());
				data.Add(new WebSocketBinaryFrame(new byte[0]));

				byte[][] binPayloads = new byte[][] {
					new byte[32],
					new byte[125],
					new byte[126],
					new byte[127],
					new byte[sizeof(ushort) - 1],
					new byte[sizeof(ushort)],
					new byte[sizeof(ushort) + 1],
					new byte[sizeof(ushort) * 2]
				};

				// Binary frames
				foreach (byte[] p in binPayloads)
				{
					data.Add(new WebSocketBinaryFrame(p));
				}

				// Binary frames masked
				foreach (byte[] p in binPayloads)
				{
					data.Add(new WebSocketBinaryFrame());
				}

				// Control frames

				// - Ping Frames
				data.Add(new WebSocketPingFrame());
				data.Add(new WebSocketPingFrame(new byte[0]));
				foreach (byte[] p in binPayloads)
				{
					data.Add(new WebSocketPingFrame(p));
				}

				// - Pong Frames
				data.Add(new WebSocketPongFrame());
				data.Add(new WebSocketPongFrame(new byte[0]));
				foreach (byte[] p in binPayloads)
				{
					data.Add(new WebSocketPongFrame(p));
				}

				// - Close frames
				data.Add(new WebSocketCloseFrame());
				data.Add(new WebSocketCloseFrame());
				WebSocketCloseFrame cf = new WebSocketCloseFrame(WebSocketCloseCode.ProtocolError, "Some close reason");
				data.Add(cf);

				return data;
			}
		}

		public static TheoryData<WebSocketTextFrame> GetTextFrames
		{
			get
			{
				TheoryData<WebSocketTextFrame> data = new TheoryData<WebSocketTextFrame>();

				data.Add(new WebSocketTextFrame(""));
				data.Add(new WebSocketTextFrame("second text"));
				data.Add(new WebSocketTextFrame("test number 3"));
				data.Add(new WebSocketTextFrame("Test_4"));

				string longText = @"Spicy jalapeno bacon ipsum dolor amet laborum pastrami voluptate quis. Short ribs ground round nisi sed commodo corned beef.
										Id reprehenderit pork quis tongue ham hock nostrud lorem jerky. Reprehenderit frankfurter leberkas tri-tip shank aliquip.";

				data.Add(new WebSocketTextFrame(longText));

				return data;
			}
		}

		/// <summary>
		/// Returns a set of byte[] representing WebSocket frames that were taken directly from the RFC 6455 document
		/// https://tools.ietf.org/html/rfc6455#section-5.7
		/// </summary>
		/// <value></value>
		public static TheoryData<byte[]> GetRFCTestExamples
		{
			get
			{
				TheoryData<byte[]> data = new TheoryData<byte[]>();

				// A single-frame unmasked text message
				data.Add(new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f }); // (contains "Hello")

				// A single-frame masked text message
				data.Add(new byte[] { 0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58 }); // (contains "Hello")

				return data;
			}
		}
	}
}
