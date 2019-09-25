using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using System.Net;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
    public class WebSocket_Tests
    {
    	private readonly ITestOutputHelper output;

		public WebSocket_Tests(ITestOutputHelper o)
		{
			this.output = o;
		}

        [Fact]
        public void WebSocketFrame_ApplyMask_ExpectedOutput()
        {
            // Arbitrarily chosen bytes
            // Masking key is supposed to be cryptographically secure and crafted from
            // a high entropy randomness source but for tests it's fine if it's hardcoded
            byte[] k = new byte[4] { 0b01101001, 0b11100001, 0b11010001, 0b00011010 };

            // Arbitrarily chosen bytes that represent data
            byte[] plain = new byte[6] { 0b11011010, 0b11110011, 0b01010010, 0b00010011, 0b00001111, 0b11111111 };

            byte[] expectedMasked = new byte[plain.Length];

            // Hardcode expected output to avoid error that may arise from an algorithm
            expectedMasked[0] = (byte) (plain[0] ^ k[0]); 
            expectedMasked[1] = (byte) (plain[1] ^ k[1]); 
            expectedMasked[2] = (byte) (plain[2] ^ k[2]); 
            expectedMasked[3] = (byte) (plain[3] ^ k[3]); 
            expectedMasked[4] = (byte) (plain[4] ^ k[0]); 
            expectedMasked[5] = (byte) (plain[5] ^ k[1]); 

            byte[] masked = WebSocketFrame.ApplyMask(plain, k);
            
            Assert.True(expectedMasked.SequenceEqual(masked), "Masking algorithm didn't produce expected output");
        }

        [Fact]
        public void WebSocketFrame_ApplyMask_RevertInput()
        {
            Random rnd = new Random();

            byte[] original_rndContent = new byte[100];
            rnd.NextBytes(original_rndContent);

            byte[] k = new byte[4];
            rnd.NextBytes(k);

            byte[] masked = WebSocketFrame.ApplyMask(original_rndContent, k);
            byte[] reverted = WebSocketFrame.ApplyMask(masked, k);

            Assert.True(original_rndContent.SequenceEqual(reverted), "The masking algorithm didn't yeild the original data when applying the algorithm on masked data");
        }
		
        public static TheoryData<WebSocketFrame> GetTestFrames
        {
            get
            {
                TheoryData<WebSocketFrame> data = new TheoryData<WebSocketFrame>();

                data.Add(new WebSocketDataFrame(true, true, System.Text.Encoding.UTF8.GetBytes(""), WebSocketDataFrame.DataFrameType.Text));
                data.Add(new WebSocketDataFrame(false, true, System.Text.Encoding.UTF8.GetBytes("second text"), WebSocketDataFrame.DataFrameType.Text));
                data.Add(new WebSocketDataFrame(true, false, System.Text.Encoding.UTF8.GetBytes("test number 3"), WebSocketDataFrame.DataFrameType.Text));
                data.Add(new WebSocketDataFrame(false, false, System.Text.Encoding.UTF8.GetBytes("Test_4"), WebSocketDataFrame.DataFrameType.Text));

				string longText = @"Spicy jalapeno bacon ipsum dolor amet laborum pastrami voluptate quis. Short ribs ground round nisi sed commodo corned beef.
									Id reprehenderit pork quis tongue ham hock nostrud lorem jerky. Reprehenderit frankfurter leberkas tri-tip shank aliquip.";

                data.Add(new WebSocketDataFrame(true, false, System.Text.Encoding.UTF8.GetBytes(longText), WebSocketDataFrame.DataFrameType.Text));

                data.Add(new WebSocketDataFrame(true, true, System.Text.Encoding.UTF8.GetBytes("Test_5"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(false, true, System.Text.Encoding.UTF8.GetBytes("Test_6"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(true, false, System.Text.Encoding.UTF8.GetBytes("Test_7"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(false, false, System.Text.Encoding.UTF8.GetBytes("Test_8"), WebSocketDataFrame.DataFrameType.Binary));

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(GetTestFrames))]
        public void WebSocketFrame_GetBytes(WebSocketFrame frame)
        {
            byte[] bytes = frame.GetBytes();

			output.WriteLine(String.Format("Testing frame with payload (length {0}) {1}", frame.Plaintext.Length, frame.Plaintext));
			output.WriteLine("Bytes: " + BitConverter.ToString(bytes));

            Assert.True((bytes[0] >> 7 == Convert.ToInt32(frame.Fin)), "Frame FIN bit was not set properly");
            Assert.True((byte) (bytes[0] & 0b00001111) == frame.OpCode, "Frame OPCode was not set properly");
            Assert.True((bytes[1] >> 7) == Convert.ToInt32(frame.Masked), "Frame MASKED bit was not set properly");

			byte l = (byte) (bytes[1] & 0b01111111);
			int frameHeaderSize = 2;
			int length;
			if (l <= 125)
			{
				length = l;
			}
			else if (l <= 126)
			{
				byte[] lengthBytes = new byte[2];
				frameHeaderSize = frameHeaderSize + 2;
				Array.Copy(bytes, 2, lengthBytes, 0, 2);
				length = BitConverter.ToUInt16(lengthBytes, 0);
			}
			else
			{
				byte[] lengthBytes = new byte[4];
				frameHeaderSize = frameHeaderSize + 4;
				bytes.CopyTo(lengthBytes, 0);
				length = (int) BitConverter.ToUInt32(lengthBytes);
			}

            int messageBytesLength = System.Text.Encoding.UTF8.GetBytes(frame.Plaintext).Length;

            Assert.Equal(length, messageBytesLength);
            Assert.True(bytes.Length == frameHeaderSize + length, "Frame object did not return the correct number of bytes");

            byte[] realContent = new byte[length];
            Array.Copy(bytes, frameHeaderSize, realContent, 0, length);

            string read = System.Text.Encoding.UTF8.GetString(realContent);
            Assert.Equal(frame.Plaintext, read);
        }


        [Fact]
        public void WebSocketFrame_TryParse_Valid_DataFrame_Text_SingleFrame()
        {
            string testContent = "test!";

            bool fin = true;
            WebSocketOPCode OPCode = WebSocketOPCode.Text;
            byte[] payload = System.Text.Encoding.UTF8.GetBytes(testContent);



            // Manually craft a WebSocketFrame
            Random rnd = new Random();
            byte[] maskingK = new byte[4];
            // The masking key is supposed to be cryptographically secure but this will suffice for testing purposes
            rnd.NextBytes(maskingK);

            
        }
    }
}
