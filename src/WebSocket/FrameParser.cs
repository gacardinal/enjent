using System;
using System.Net.Sockets;

namespace NarcityMedia.Enjent
{
    abstract partial class WebSocketFrame
    {
          
        /// <summary>
        /// Tries to parse a byte[] header into a SocketDataFrame object.
        /// Returns a null reference if object cannot be parsed
        /// </summary>
        /// <param name="headerBytes">A byte array containing the read frame header bytes</param>
        /// <param name="socket">A reference to the socket from which to parse the SocketFrame</param>
        /// <returns>
        /// If pasrse is successful, an Object of a type that is derived from SocketFrame.abstract Returns a null pointer otherwise.
        /// </returns>
        /// <remarks>
        /// This method is not exactly like the Int*.TryParse() methods as it doesn't take an 'out' parameter and return a
        /// boolean value but rather returns either the parsed object reference or a null reference, which means that callers of this method need to check
        /// for null before using the return value.
        /// Furthermore, if the parse is successful, a caller should check the type of the object that is returned to, for example,
        /// differenciate between a SocketDataFrame and a SocketControlFrame, which are both derived from SocketFrame.
        /// It is necessary to pass a reference to the socket because of the way the WebSocket protocol is made.abstract It is impossible to know
        /// the length of the frame before having parsed the headers hence it is possible that more bytes will need to be read from the socket buffer.
        /// </remarks>
        public static WebSocketFrame? TryParse(byte[] headerBytes, Socket socket)
        {
            int headerSize = headerBytes.Length;
            bool fin = (headerBytes[0] >> 7) != 0;
            WebSocketOPCode opcode = (WebSocketOPCode) ((byte) (headerBytes[0] & 0b00001111));
            bool masked = (headerBytes[1] & 0b10000000) != 0;
            ushort contentLength = (ushort) (headerBytes[1] & 0b01111111);

            if (contentLength <= 126)
            {
                if (contentLength == 126)
                {
                    headerSize = 4;
                    byte[] largerHeader = new byte[headerSize];
                    headerBytes.CopyTo(largerHeader, 0);
                    // Read next two bytes and interpret them as the content length
                    socket.Receive(largerHeader, 2, 2, SocketFlags.None);
                    contentLength = (ushort) (largerHeader[2] << 8 | largerHeader[3]);
                }

                byte[] maskingKey = new byte[4];
                socket.Receive(maskingKey);

                byte[] contentBuffer = new byte[contentLength];
                if (contentLength > 0) socket.Receive(contentBuffer);

                WebSocketFrame frame;
                if (opcode == WebSocketOPCode.Text || opcode == WebSocketOPCode.Binary)
                {
                    frame = new WebSocketDataFrame(fin, masked, ApplyMask(contentBuffer, maskingKey), (WebSocketDataFrame.DataFrameType) opcode);
                }
                else if (opcode == WebSocketOPCode.Close)
                {
                    WebSocketCloseFrame closeFrame = new WebSocketCloseFrame();
                    if (contentLength >= 2)
                    {
                        byte[] unmasked = ApplyMask(contentBuffer, maskingKey);
                        WebSocketCloseCode closeCode = (WebSocketCloseCode) BitConverter.ToUInt16(unmasked);
                        closeFrame.CloseCode = closeCode;

                        if (contentLength > 2)
                        {
                            byte[] closeReasonBytes = new byte[contentLength - 2];
                            Array.Copy(contentBuffer, 2, closeReasonBytes, 0, closeReasonBytes.Length);
                            closeFrame.CloseReason = System.Text.Encoding.UTF8.GetString(closeReasonBytes);
                        }
                    }

                    frame = closeFrame;
                }
                else
                {
                    frame = new WebSocketControlFrame(fin, masked, (WebSocketOPCode) opcode);
                }

                return frame;
            }

            return null;
        }

        /// <summary>
        /// Applies the masking / unmasking algorithm defined in section 5.3 of RFC6455.
        /// </summary>
        /// <param name="data">Masked payload bytes</param>
        /// <param name="maskingKey">Unmasking key bytes</param>
        /// <returns>The masked / unmasked data</returns>
        /// <remarks>
        /// This algorithm is such that passing the output of this function to itself with the same masking key
        /// will yeild the original result because it essentially performs an XOR operation on the data with the masking key.
        /// Reffer to section 5.3 of RFC6455 for full implementation details
        /// </remarks>
        public static byte[] ApplyMask(byte[] data, byte[] maskingKey)
        {
            if (maskingKey.Length != 4) throw new ArgumentException("Masking key must always be of length 4");

            if (data.Length == 0) return new byte[0];

			return ApplyMaskBlock(data, maskingKey);
        }

        
		private static byte[] ApplyMaskBlock(byte[] data, byte[] maskingKey)
        {
			if (data.Length < 4)
			{
				uint fullMaskingKey = (uint) ((maskingKey[0] << 24) | (maskingKey[1] << 16) | (maskingKey[2] << 8) | maskingKey[3]);
				byte b0, b1, b2, b3;
				byte[] result = new byte[data.Length];
				for (uint i = 0; i < data.Length; i = i + 4)
				{
					if (data.Length - i >= 4)
					{
						b0 = data[i];		b1 = data[i + 1];
						b2 = data[i + 2];	b3 = data[i + 3];

						uint fourByteBlock = (uint) ((b0 << 24) | (b1 << 16) | (b3 << 8) | b3);

						uint blockResult = fourByteBlock ^ fullMaskingKey;
						
						result[i + 3]	= (byte) (blockResult & 0x000F);
						result[i + 2]	= (byte) (blockResult >> 24 & 0x000F);
						result[i + 1]	= (byte) (blockResult >> 16 & 0x000F);
						result[i]		= (byte) (blockResult >> 8 & 0x000F);
					}
					else
					{
						byte[] lastBytes = ApplyMaskOneByOne(data[6..], maskingKey);
						lastBytes.CopyTo(result, data.Length - i);
					}
				}

				return result;
			}
			else
			{
				return ApplyMaskOneByOne(data, maskingKey);
			}
        }

		private static byte[] ApplyMaskOneByOne(byte[] data, byte[] maskingKey)
		{
			byte[] unmasked = new byte[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                unmasked[i] = (byte) (data[i] ^ maskingKey[i % 4]);
            }

            return unmasked;
		}
    }
}
