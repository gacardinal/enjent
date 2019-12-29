using System;
using System.Linq;
using System.IO;
using Xunit;
using Xunit.Sdk;
using Xunit.Abstractions;
using NarcityMedia.Enjent;

namespace EnjentUnitTests.WebSocket
{

    /// <summary>
    /// Tests features specific to the WebSocketFrame class
    /// </summary>
    public partial class WebSocketFrameTest : EnjentTest
    {
        private static Random rand = new Random();

		public WebSocketFrameTest(ITestOutputHelper o) : base(o)
		{}

		[Fact]
		public void WebSocketFrame_FIN_Init()
		{
			WebSocketFrameConcretion f = new WebSocketFrameConcretion(false, false, new byte[0]);
			Assert.True(f.Fin == false, "WebSocketFrame.Fin was not correctly initialized");

			WebSocketFrameConcretion f2 = new WebSocketFrameConcretion(true, false, new byte[0]);
			Assert.True(f2.Fin == true, "WebSocketFrame.Fin was not correctly initialized");
		}

		[Fact]
		public void WebSocketFrame_MASKED_Init()
		{
			WebSocketFrameConcretion f = new WebSocketFrameConcretion(false, false, new byte[0]);
			Assert.True(f.Masked == false, "WebSocketFrame.Masked was not correctly initialized");

			WebSocketFrameConcretion f2 = new WebSocketFrameConcretion(false, true, new byte[0]);
			Assert.True(f2.Masked == true, "WebSocketFrame.Masked was not correctly initialized");
		}

		[Fact]
		public void WebSocketFrame_PAYLOAD_Init()
		{
			WebSocketFrameConcretion f = new WebSocketFrameConcretion(false, false, new byte[0]);

			byte[] oneBytePayload = new byte[1];
			rand.NextBytes(oneBytePayload);
			WebSocketFrameConcretion f2 = new WebSocketFrameConcretion(false, false, oneBytePayload);

			byte[] ratherLargePayload = new byte[256];
			rand.NextBytes(ratherLargePayload);
			WebSocketFrameConcretion f3 = new WebSocketFrameConcretion(false, false, ratherLargePayload);
			
			Assert.True(f.Payload.SequenceEqual(new byte[0]), "WebSocketDataFrame payload was not initialized correctly");
			Assert.True(f2.Payload.SequenceEqual(oneBytePayload), "WebSocketDataFrame payload was not initialized correctly");
			Assert.True(f3.Payload.SequenceEqual(ratherLargePayload), "WebSocketDataFrame payload was not initialized correctly");
		}


		[Fact]
		public void WebSocketFrame_NULL_PAYLOAD_Init()
		{
			WebSocketFrameConcretion f = new WebSocketFrameConcretion(false, false, null);

			Assert.True(new byte[0].SequenceEqual(f.Payload), "WebSOcketFrame received null as payload constructor argument but did not default Payload to empty byte array");
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
        /// <summary>
        /// When the masking algorithm is applied on plain data (d1) with a given key (k),
        /// pssing the ciphered data (c) to the algorithm with the same key should yeild the original plain data
        /// such that:
        ///     c = mask(d1, k),
        ///     d2 = mask(c, k) = d1
        /// </summary>
        public void WebSocketFrame_ApplyMask_RevertInput()
        {
            byte[] original_rndContent = new byte[100];
            rand.NextBytes(original_rndContent);

            // No need for cryptographically secure randomness here
            byte[] k = new byte[4];
            rand.NextBytes(k);

            byte[] masked = WebSocketFrame.ApplyMask(original_rndContent, k);
            byte[] reverted = WebSocketFrame.ApplyMask(masked, k);

            Assert.True(original_rndContent.SequenceEqual(reverted), "The masking algorithm didn't yeild the original data when applying the algorithm on masked data");
        }
		
        [Theory]
        [MemberData(nameof(GetBytes_WebSocketFrame_Subtypes))]
        public void WebSocketTextFrame_GetBytes(WebSocketFrame frame)
        {
			byte[] frameBytes = frame.GetBytes();

			WebSocketFrame parsedFrame = WebSocketFrame.Parse(new MemoryStream(frameBytes));

			Assert.Equal(frame.Fin, parsedFrame.Fin);
			Assert.Equal(frame.Masked, parsedFrame.Masked);
			Assert.Equal(frame.OpCode, parsedFrame.OpCode);
			Assert.Equal(frame.Payload, parsedFrame.Payload);
			if (frame.Masked)
			{
				Assert.Equal(frame.MaskingKey, parsedFrame.MaskingKey);
			}

			// byte[] frameBytes = frame.GetBytes();

			// bool actualFin = (byte)(frameBytes[0] >> 7) == 1;
			// WebSocketOPCode actualOpCode = (WebSocketOPCode)(frameBytes[0] & 0b00001111);
			// bool actualMasked = (frameBytes[1] >> 7) == 1;

			// Assert.True(actualFin == frame.Fin, "Frame FIN bit was not set properly");
			// Assert.True(actualOpCode == frame.OpCode, "Frame OPCode bits were not set properly");
			// Assert.True(actualMasked == frame.Masked, "Frame MASKED bit was not set properly");

			// byte l = (byte)(frameBytes[1] & 0b01111111);
			// int frameHeaderSize = 2;
			// int length;
			// if (l <= 125)
			// {
			// 	length = l;
			// }
			// else if (l <= 126)
			// {
			// 	byte[] lengthBytes = new byte[2];
			// 	frameHeaderSize = frameHeaderSize + 2;
			// 	Array.Copy(frameBytes, 2, lengthBytes, 0, 2);
			// 	lengthBytes.ReverseIfLittleEndian();
			// 	length = BitConverter.ToUInt16(lengthBytes, 0);
			// }
			// else
			// {
			// 	byte[] lengthBytes = new byte[4];
			// 	frameHeaderSize = frameHeaderSize + 4;
			// 	Array.Copy(frameBytes, 2, lengthBytes, 0, 4);
			// 	lengthBytes.ReverseIfLittleEndian();
			// 	length = (int)BitConverter.ToUInt32(lengthBytes);
			// }

			// byte[] maskingKey = new byte[4];
			// if (actualMasked)
			// {
			// 	Array.Copy(frameBytes, frameHeaderSize, maskingKey, 0, 4);
			// }

			// Assert.True(length == frame.Payload.Length, "Encoded payload length did not match actual payload length");
			// Assert.True(frameBytes.Length == frameHeaderSize + length + (actualMasked ? maskingKey.Length : 0), "Frame object did not return the correct number of bytes");

			// byte[] actualContent = new byte[length];
			// Array.Copy(frameBytes, frameHeaderSize + (actualMasked ? maskingKey.Length : 0), actualContent, 0, length);
			// Assert.True(Enumerable.SequenceEqual(actualMasked ? WebSocketFrame.ApplyMask(frame.Payload, maskingKey) : frame.Payload, actualContent), "'Real' frame payload did not match the frame's payload property");
        }
	}
}
