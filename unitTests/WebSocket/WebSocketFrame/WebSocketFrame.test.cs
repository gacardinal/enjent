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

		public WebSocketFrameTest(ITestOutputHelper o) : base(o)
		{}

		[Fact]
		public void WebSocketFrame_PAYLOAD_Init()
		{
			WebSocketFrameConcretion f = new WebSocketFrameConcretion(new byte[0]);

			byte[] oneBytePayload = new byte[1];
			rand.NextBytes(oneBytePayload);
			WebSocketFrameConcretion f2 = new WebSocketFrameConcretion(oneBytePayload);

			byte[] ratherLargePayload = new byte[256];
			rand.NextBytes(ratherLargePayload);
			WebSocketFrameConcretion f3 = new WebSocketFrameConcretion(ratherLargePayload);
			
			Assert.True(f.Payload.SequenceEqual(new byte[0]), "WebSocketDataFrame payload was not initialized correctly");
			Assert.True(f2.Payload.SequenceEqual(oneBytePayload), "WebSocketDataFrame payload was not initialized correctly");
			Assert.True(f3.Payload.SequenceEqual(ratherLargePayload), "WebSocketDataFrame payload was not initialized correctly");
		}

		[Fact]
		public void WebSocketFrame_NULL_PAYLOAD_Init()
		{
			Assert.Throws<ArgumentNullException>("payload", () => {
				WebSocketFrameConcretion f = new WebSocketFrameConcretion(null);
			});
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
        }

		public void CompareFrames(WebSocketFrame a, WebSocketFrame b)
		{
			Assert.Equal(a.Fin, b.Fin);
			Assert.Equal(a.Masked, b.Masked);
			Assert.Equal(a.OpCode, b.OpCode);
			Assert.Equal(a.Payload, b.Payload);
			if (a.Masked)
			{
				Assert.Equal(a.MaskingKey, b.MaskingKey);
			}
		}
	}
}
