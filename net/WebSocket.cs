using System;
using System.Numerics;
using System.Collections.Generic;

namespace NarcityMedia.Net
{
    /// <summary>
    /// Represents a general concept of a WebSocket frame described by the 
    /// WebSocket standard
    /// </summary>
    abstract partial class SocketFrame
    {
        public enum OPCodes
        {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xA
        }

        protected bool fin;
        protected bool masked;
        protected byte[] _data;
        protected byte[] data
        {
            get { return this._data; }
            set
            {
                this._data = value;
                this.Plaintext = System.Text.Encoding.UTF8.GetString(value);
            }
        }
        public byte opcode;
        protected ushort contentLength;

        public string Plaintext;

        public SocketFrame(bool fin, bool masked, ushort length)
        {
            this.fin = fin;
            this.masked = masked;
            this.contentLength = length;
        }

        /// <summary>
        /// Initializes the OPCode of the frame, the frame must decide it's own opcode
        /// </summary>
        /// <remarks>Derived classes MUST override</remarks>
        protected abstract void InitOPCode();

        /// <summary>
        /// Returns the bytes representation of the data frame
        /// </summary>
        /// <returns>Returns the bytes that form the data frame</returns>
        public byte[] GetBytes()
        {
            byte[] frameHeader = new byte[2];

            // First octet - 1 bit for FIN, 3 reserved, 4 for OP Code
            byte octet0 = (byte) ((this.fin) ? 0b10000000 : 0b00000000);
            octet0 = (byte) (octet0 | this.opcode);

            byte octet1 = (byte) ((this.masked) ? 0b10000000 : 0b00000000);
            octet1 = (byte) (octet1 | this.contentLength);
            // octet1 = (byte) (octet1 | this.contentLength);

            frameHeader[0] = octet0;
            frameHeader[1] = octet1;

            if (this.data != null)
            {
                byte[] finalBytes = AppendMessageToBytes(frameHeader, this.data);
                return finalBytes;
            }

            return frameHeader;
        }

        /// <summary>
        /// Appends the payload to the header of the data frame
        /// </summary>
        /// <param name="header">Bytes of the frame header</param>
        /// <param name="message">Bytes of the frame message</param>
        /// <returns>All the bytes of the frame</returns>
        private byte[] AppendMessageToBytes(byte[] header, byte[] message)
        {
            byte[] payload = new byte[header.Length + message.Length];
            header.CopyTo(payload, 0);
            message.CopyTo(payload, header.Length);

            // Removing trailing zeroes
            int i = payload.Length - 1;
            while (payload[i] == 0)
            {
                i--;
            }

            byte[] trimmed = new byte[i + 1];
            Array.Copy(payload, trimmed, i + 1);

            return trimmed;
        }
    }

    /// <summary>
    /// Represents a WebSocket frame that contains data
    /// </summary>
    class SocketDataFrame : SocketFrame
    {
        public enum DataFrameType { Text, Binary }
        public DataFrameType DataType;

        public SocketDataFrame() : base(true, true, 0)
        {
        }

        public SocketDataFrame(bool fin, bool masked, ushort length,
                                DataFrameType dataType) : base(fin, masked, length)
        {
            this.DataType = dataType;
            this.InitOPCode();
        }
        public SocketDataFrame(bool fin, bool masked, ushort length,
                                DataFrameType dataType,
                                byte[] message) : base(fin, masked, length)
        {
            this.DataType = dataType;
            this.InitOPCode();

            if (message.Length <= 65536)
            {
                this.data = message;
            }
            else
            {
                throw new ArgumentOutOfRangeException("This class does not support frames that have a content length value that is greater than 126");
            }
        }

        protected override void InitOPCode()
        {
            if (this.DataType == DataFrameType.Text) this.opcode = (byte) SocketFrame.OPCodes.Text;
            else this.opcode = (byte) SocketFrame.OPCodes.Binary;
        }
    }

    class SocketControlFrame : SocketDataFrame
    {
        public SocketControlFrame(SocketFrame.OPCodes controlOpCode) :base(true, false, 0,SocketDataFrame.DataFrameType.Binary)
        {
            this.opcode = (byte) controlOpCode;
        }

        public SocketControlFrame(bool fin, bool masked, SocketFrame.OPCodes controlOpCode)
                : base(true, false, 0, SocketDataFrame.DataFrameType.Binary)
        {
            this.opcode = (byte) controlOpCode;
        }
    }
}

/*
    0                   1                   2                   3
    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    +-+-+-+-+-------+-+-------------+-------------------------------+
    |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
    |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
    |N|V|V|V|       |S|             |   (if payload len==126/127)   |
    | |1|2|3|       |K|             |                               |
    +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
    |     Extended payload length continued, if payload len == 127  |
    + - - - - - - - - - - - - - - - +-------------------------------+
    |                               |Masking-key, if MASK set to 1  |
    +-------------------------------+-------------------------------+
    | Masking-key (continued)       |          Payload Data         |
    +-------------------------------- - - - - - - - - - - - - - - - +
    :                     Payload Data continued ...                :
    + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
    |                     Payload Data continued ...                |
    +---------------------------------------------------------------+

    OPCODES
        0x0 : Continuation
        0x1 : Text (UTF-8)
        0x2 : Binary
        0x8 : Close
        0x9 : Ping
        0xA : Pong
 */
