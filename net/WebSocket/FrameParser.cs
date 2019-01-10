using System;
using System.Net.Sockets;

namespace NarcityMedia.Net
{
    abstract partial class SocketFrame
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
        public static SocketFrame TryParse(byte[] headerBytes, Socket socket)
        {
            int headerSize = headerBytes.Length;
            bool fin = (headerBytes[0] >> 7) != 0;
            byte opcode = (byte) (headerBytes[0] & 0b00001111);
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

                SocketFrame frame;
                if (opcode == 1 || opcode == 2)
                    frame = new SocketDataFrame(fin, masked, contentLength, (SocketDataFrame.DataFrameType) opcode, UnmaskContent(contentBuffer, maskingKey));
                else
                    frame = new SocketControlFrame(fin, masked, (SocketFrame.OPCodes)opcode);

                return frame;
            }

            return null;
        }

        private static byte[] UnmaskContent(byte[] masked, byte[] maskingKey)
        {
            if (maskingKey.Length != 4) throw new ArgumentException("Masking key must always be of length 4");

            if (masked.Length == 0) return new byte[0];

            byte[] unmasked = new byte[masked.Length];

            for (int i = 0; i < masked.Length; i++)
            {
                unmasked[i] = (byte) (masked[i] ^ maskingKey[i % 4]);
            }

            return unmasked;
        }
    }
}