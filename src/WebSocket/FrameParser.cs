using System;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace NarcityMedia.Enjent.WebSocket
{
    abstract partial class WebSocketFrame
    {
		/// <summary>
		/// Tries to parse the given bytes as a <see cref="WebSocketFrame" />.
		/// </summary>
		/// <param name="bytes">The bytes to parse</param>
        /// <param name="result">A reference to a <cee cref="WebSocketFrame" /> that will contain the result of the parsing operation</param>
		/// <returns>True if the parsing is successful, false otherwise</returns>
		public static bool TryParse(byte[] bytes, out WebSocketFrame? result)
		{
			MemoryStream memStream = new MemoryStream(bytes);
			return WebSocketFrame.TryParse(memStream, out result);
		}

		/// <summary>
		/// Tries to parse the given bytes as a <see cref="WebSocketFrame" />.
		/// </summary>
		/// <param name="bytes">The socket from which to read the bytes to parse</param>
        /// <param name="result">A reference to a <cee cref="WebSocketFrame" /> that will contain the result of the parsing operation</param>
		/// <returns>True if the parsing is successful, false otherwise</returns>
		public static bool TryParse(Socket inputSocket, out WebSocketFrame? result)
		{
			NetworkStream networkStream = new NetworkStream(inputSocket);
			return WebSocketFrame.TryParse(networkStream, out result);
		}

        /// <summary>
        /// Tries to parse a byte[] heaheaderBytesder into a SocketDataFrame object.
        /// Returns a null reference if object cannot be parsed
        /// </summary>
        /// <param name="input">A reference to a <cee cref="System.IO.Stream" /> from which to parse the SocketFrame</param>
        /// <param name="result">A reference to a <cee cref="WebSocketFrame" /> that will contain the result of the parsing operation</param>
        /// <returns>
        /// If pasrse is successful, true, false otherwise.
		/// If parsing is successful, the result parameter is affected a reference to an Object of a type
		/// that is derived from <see cref="WebSocketFrame" />. or null if the parsing fails.
        /// </returns>
        /// <remarks>
        /// If the parsing is successful, a caller should check the type of the object that is returned too as to, for example,
        /// differenciate between a <see cref="TF" /> and a <see cref="WebSocketControlFrame" />,
		/// are both derived from <see cref="WebSocketFrame" />.
		/// </remarks>
        public static bool TryParse(Stream input, out WebSocketFrame? result)
        {
			try
			{
				result = WebSocketFrame.Parse(input);
				return true;
			}
			catch (Exception)
			{
				result = null;
				return false;
			}
        }

		public static WebSocketFrame Parse(Stream input)
		{
			byte[] headerBytes = new byte[2];
			input.Read(headerBytes);

			return WebSocketFrame.Parse(input, headerBytes);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		/// <remarks>
        /// Consider passing an instance of type <see cref="NetworkStream" /> or <see cref="MemoryStream" /> as the input parameter
		/// </remarks>
		public static WebSocketFrame Parse(Stream input, byte[] header)
		{
			if (header.Length != 2)
				throw new ArgumentException("Header must have a length of exactly 2", nameof(header));

            int headerSize = header.Length;
            bool fin = (header[0] >> 7) != 0;
            WebSocketOPCode opcode = (WebSocketOPCode) ((byte) (header[0] & 0b00001111));
            bool masked = (header[1] & 0b10000000) != 0;
            ushort contentLength = (ushort) (header[1] & 0b01111111);
			byte[] maskingKey = new byte[4];

			if (contentLength > 0 && contentLength <= 126)
			{
				if (contentLength == 126)
				{
					headerSize = 4;
					byte[] largerHeader = new byte[headerSize];
					header.CopyTo(largerHeader, 0);
					// Read next two bytes and interpret them as the content length
					input.Read(largerHeader, 2, 2);
					// input.Receive(largerHeader, 2, 2, SocketFlags.None);
					contentLength = (ushort) (largerHeader[2] << 8 | largerHeader[3]);
				}

				if (masked)
				{
					input.Read(maskingKey);
				}

			}

			byte[] contentBuffer = new byte[contentLength];
			input.Read(contentBuffer);

			if (masked)
			{
				contentBuffer = ApplyMask(contentBuffer, maskingKey);
			}

			WebSocketFrame? frame = null;
			switch (opcode)
			{
				case WebSocketOPCode.Continuation:
					frame = new WebSocketContinuationFrame(contentBuffer);
					break;
				case WebSocketOPCode.Binary:
					frame = new WebSocketBinaryFrame(contentBuffer);
					break;
				case WebSocketOPCode.Text:
					try
					{
						string text = Encoding.UTF8.GetString(contentBuffer);
						frame = new WebSocketTextFrame(text);
					}
					catch (Exception e)
					{
						throw new EnjentWebSocketProtocolException("Got invalid UTF8 data", e);
					}
					break;
				case WebSocketOPCode.Close:
					frame = ParseCloseFrame(contentBuffer);
					break;
				case WebSocketOPCode.Ping:
					frame = new WebSocketPingFrame(contentBuffer);
					break;
				case WebSocketOPCode.Pong:
					frame = new WebSocketPongFrame(contentBuffer);
					break;
				default:
					throw new EnjentWebSocketProtocolException("Unknown WebSocket OpCode " + opcode.ToString());
			}

			frame.Fin = fin;

			if (masked)
			{
				frame.Masked = true;
				frame.MaskingKey = maskingKey;
			}

			return frame;
		}

		private static WebSocketFrame ParseCloseFrame(byte[] contentBuffer)
		{
			WebSocketCloseFrame closeFrame = new WebSocketCloseFrame();
			if (contentBuffer.Length >= 2)
			{
				byte[] closeCodeBytes = new byte[2] { contentBuffer[0], contentBuffer[1] };
				if (BitConverter.IsLittleEndian)
				{
					// Close codes are transmitted in network byte order (big endian)
					Array.Reverse(closeCodeBytes);
				}

				int maybeCloseCode = BitConverter.ToInt16(contentBuffer);
				if (System.Enum.IsDefined(typeof(WebSocketCloseCode), maybeCloseCode))
				{
					WebSocketCloseCode closeCode = (WebSocketCloseCode) maybeCloseCode;
					closeFrame.CloseCode = closeCode;

					if (contentBuffer.Length > 2)
					{
						byte[] closeReasonBytes = new byte[contentBuffer.Length - 2];
						Array.Copy(contentBuffer, 2, closeReasonBytes, 0, closeReasonBytes.Length);
						closeFrame.SetCloseReason(closeCode, System.Text.Encoding.UTF8.GetString(closeReasonBytes));
					}
				}
				else
				{
					throw new EnjentWebSocketProtocolException("Close frame specified a close reason but included an invalid close code");
				}
			}
			else
			{
				closeFrame = new WebSocketCloseFrame(WebSocketCloseCode.NoCloseCode);
			}

			return closeFrame;
		}

		/// <summary>
		/// Applies the masking / unmasking algorithm defined in section 5.3 of RFC6455.
		/// </summary>
		/// <param name="data">Data on which to apply the masking algorithm</param>
		/// <param name="maskingKey">Masking key bytes</param>
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

        /// <summary>
        /// Applies the masking algorithm defined by RFC6455 for some given data with a given maskingKey by operating on 'blocks' of data.
        /// </summary>
        /// <param name="data">Data on which to apply the masking algorithm</param>
        /// <param name="maskingKey">Masking key bytes</param>
        /// <returns>The masked / unmasked data</returns>
        /// <remarks>
        /// /// This function treats the maskingKey as an int rather than a 4 bytes array. It then iterates over the data in chunks of 4 bytes
        /// and treats each chunk as an int also. It then performs the XOR operation on two ints rather than 2 bytes.
        /// </remarks>
		private static byte[] ApplyMaskBlock(byte[] data, byte[] maskingKey)
        {
			if (data.Length < 4)
			{
				uint fullMaskingKey = (uint) ((maskingKey[0] << 24) | (maskingKey[1] << 16) | (maskingKey[2] << 8) | maskingKey[3]);
				byte b0, b1, b2, b3;
				byte[] result = new byte[data.Length];
				for (int i = 0; i < data.Length; i = i + 4)
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
						byte[] lastBytes = ApplyMaskOneByOne(data[i..], maskingKey);
						lastBytes.CopyTo(result, i);
					}
				}

				return result;
			}
			else
			{
				return ApplyMaskOneByOne(data, maskingKey);
			}
        }

        /// <summary>
        /// Applies the masking algorithm defined by RFC6455 for some given data with a given maskingKey by operating on each individual bytes
        /// </summary>
        /// <param name="data">Data on which to apply the masking algorithm</param>
        /// <param name="maskingKey">Masking key bytes</param>
        /// <returns>The masked / unmasked data</returns>
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
