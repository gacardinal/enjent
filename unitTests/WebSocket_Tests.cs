using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using System.Net;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
    /// <summary>
    /// Tests features specific to the WebSocket protocol.
    /// Test data for this test class is defined in WebSocket_Tests_data.cs
    /// </summary>
    public partial class WebSocket_Tests
    {
    	private readonly ITestOutputHelper output;
        private static Random rand = new Random();

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

		// This is NOT being run during the test execution
		public void WebSocketFrame_ApplyMask_Benchmark()
		{
			byte[] k = new byte[4];
            rand.NextBytes(k);
			byte[] bigBuffer = new byte[1024 * 1000000];
			rand.NextBytes(bigBuffer);
			
			this.output.WriteLine("Commencing masking algorithm benchmark");
			Stopwatch watch = Stopwatch.StartNew();

			byte[] masked = WebSocketFrame.ApplyMask(bigBuffer, k);

			watch.Stop();
			long elapsedMs = watch.ElapsedMilliseconds;
			this.output.WriteLine("Completed in : " + elapsedMs.ToString());
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
        [MemberData(nameof(GetTestFrames))]
        public void WebSocketFrame_GetBytes(WebSocketDataFrame frame)
        {
            byte[] frameBytes = frame.GetBytes();

			output.WriteLine(String.Format("Testing frame with payload (length {0}) {1}", frame.Plaintext.Length, frame.Plaintext));
			output.WriteLine("Bytes: " + BitConverter.ToString(frameBytes));
    
            Assert.True((frameBytes[0] >> 7 == Convert.ToInt32(frame.Fin)), "Frame FIN bit was not set properly");
            Assert.True((byte) (frameBytes[0] & 0b00001111) == frame.OpCode, "Frame OPCode bits were not set properly");
            Assert.True((frameBytes[1] >> 7) == Convert.ToInt32(frame.Masked), "Frame MASKED bit was not set properly");

			byte l = (byte) (frameBytes[1] & 0b01111111);
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
				Array.Copy(frameBytes, 2, lengthBytes, 0, 2);
                ReverseIfLittleEndian(lengthBytes);
				length = BitConverter.ToUInt16(lengthBytes, 0);
			}
			else
			{
				byte[] lengthBytes = new byte[4];
				frameHeaderSize = frameHeaderSize + 4;
				Array.Copy(frameBytes, 2, lengthBytes, 0, 4);
                ReverseIfLittleEndian(lengthBytes);
				length = (int) BitConverter.ToUInt32(lengthBytes);
			}

            Assert.True(length == frame.Payload.Length, "Encoded payload length did not match actual payload length");
            Assert.True(frameBytes.Length == frameHeaderSize + length, "Frame object did not return the correct number of bytes");

            byte[] realContent = new byte[length];
            Array.Copy(frameBytes, frameHeaderSize, realContent, 0, length);
            Assert.True(Enumerable.SequenceEqual(frame.Payload, realContent), "'Real' frame payload did not match the frame's payload property");
        }

        /// <summary>
        /// Fields within a WebSocket frame are to be interpreted as BIG ENDIAN, however, most client architectures use little endian,
        /// in which case it is necessary to reverse an array representing a datatype such as a uint before attempting to convert
        /// this array to the said datatype.
        /// </summary>
        /// <param name="array">The array to MAYBE reverse</param>
        /// <remark>
        /// If the current architecture uses little endian, the passed array IS MODIFIED IN PLACE, else, it is untouched.
        /// </remark>
        public void ReverseIfLittleEndian(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
            {
                this.output.WriteLine("Detected architecture using little-endian, will reverse the frame length bytes since they are encoded in a WebSocket frame as big-endian");
                Array.Reverse(array);
            }
        }

        [Theory]
        [MemberData(nameof(GetRFCTestExamples))]
        public void WebSocketFrame_TryParse_Valid_DataFrame_Text_RFCExamples(byte[] bytes)
        {
            

        }
    }
}
