using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using System.Net;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
    public class WebSocket_Tests
    {

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

        public static IEnumerable<WebSocketFrame> GetTestFrames()
        {
            yield return new WebSocketDataFrame(true, true, (ushort) ("first test".Length), WebSocketDataFrame.DataFrameType.Text);
            yield return new WebSocketDataFrame(true, true, (ushort) ("first test".Length), WebSocketDataFrame.DataFrameType.Text);
            yield return new WebSocketDataFrame(true, true, (ushort) ("first test".Length), WebSocketDataFrame.DataFrameType.Text);
            yield return new WebSocketDataFrame(true, true, (ushort) ("first test".Length), WebSocketDataFrame.DataFrameType.Text);
            yield return new WebSocketDataFrame(true, true, (ushort) ("first test".Length), WebSocketDataFrame.DataFrameType.Text);
        }

        [Theory]
        [MemberData(nameof(GetTestFrames))]
        public void WebSocketFrame_GetBytes(WebSocketFrame frame)
        {
            byte[] bytes = frame.GetBytes();

            Assert.True((bytes[0] >> 7 == Convert.ToInt32(frame.fin)));
            Assert.True((byte) (bytes[0] & 0b00001111) == frame.opcode);
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
