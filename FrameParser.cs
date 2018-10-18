using System;

namespace NarcityMedia.Net
{
    abstract partial class SocketFrame
    {
        static SocketFrame FromByteArray(byte[] buffer)
        {
            if (buffer.Length >= 2)
            {
                return new SocketDataFrame(false, false, 3, SocketDataFrame.DataFrameType.Binary, new byte[1] {1});
            }
            else
            {
                throw new ArgumentException("The buffer parameter must have a Length of at least 2 to ensure the whole WebSOcket header is present.");
            }
        }
    }
}
