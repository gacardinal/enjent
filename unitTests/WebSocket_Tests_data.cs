using System;
using Xunit;
using NarcityMedia.Enjent;

namespace EnjentUnitTests
{
public partial class WebSocket_Tests
{
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

                byte[] veryLongPayload = new byte[ushort.MaxValue + 2];
                rand.NextBytes(veryLongPayload);
                data.Add(new WebSocketDataFrame(true, false, veryLongPayload, WebSocketDataFrame.DataFrameType.Binary));

                data.Add(new WebSocketDataFrame(true, true, System.Text.Encoding.UTF8.GetBytes("Test_5"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(false, true, System.Text.Encoding.UTF8.GetBytes("Test_6"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(true, false, System.Text.Encoding.UTF8.GetBytes("Test_7"), WebSocketDataFrame.DataFrameType.Binary));
                data.Add(new WebSocketDataFrame(false, false, System.Text.Encoding.UTF8.GetBytes("Test_8"), WebSocketDataFrame.DataFrameType.Binary));

                return data;
            }
        }
    }
}
