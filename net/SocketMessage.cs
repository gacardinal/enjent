using System;
using System.Numerics;
using System.Collections.Generic;

namespace NarcityMedia.Net
{
    abstract class SocketFrame
    {
        public enum OPCode
        {
            Continuation = 0x0,
            Text = 0x1,
            Binary = 0x2,
            Ping = 0x9,
            Pong = 0xA
        }

        protected bool fin;
        protected bool masked;
        protected byte[] data;
        protected byte opcode;
        protected byte contentLength;

        public SocketFrame(bool fin, bool masked, byte length)
        {
            this.fin = fin;
            this.masked = masked;
            this.contentLength = length;
        }

        protected abstract void InitOPCode();

        public byte[] GetBytes()
        {
            byte[] frameOctets = new byte[2];

            // First octet - 1 bit for FIN, 3 reserved, 4 for OP Code
            byte octet0 = (byte) ((this.fin) ? 0b10000000 : 0b00000000);
            octet0 = (byte) (octet0 | this.opcode);

            byte octet1 = (byte) ((this.masked) ? 0b10000000 : 0b00000000);
            octet1 = (byte) (octet1 | this.contentLength);
            // octet1 = (byte) (octet1 | this.contentLength);

            frameOctets[0] = octet0;
            frameOctets[1] = octet1;
            
            byte[] finalBytes = AppendMessageToBytes(frameOctets, this.data);

            Console.WriteLine("OCTETS : ");
            for (int i = 0; i < finalBytes.Length; i++)
            {
                Console.WriteLine(Convert.ToString(finalBytes[i], 2));
            }

            return frameOctets;
        }

        private byte[] AppendMessageToBytes(byte[] bytes, byte[] message)
        {
            byte[] payload = new byte[bytes.Length + message.Length];
            bytes.CopyTo(payload, 0);
            message.CopyTo(payload, bytes.Length);

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

    class SocketDataFrame : SocketFrame
    {
        public enum DataFrameType { Text, Binary }
        public DataFrameType DataType;

        public SocketDataFrame(bool fin, bool masked, byte length,
                                DataFrameType dataType,
                                byte[] message) : base(fin, masked, length)
        {
            this.DataType = dataType;
            this.InitOPCode();
            this.data = message;
        }

        protected override void InitOPCode()
        {
            if (this.DataType == DataFrameType.Text) this.opcode = (byte) SocketFrame.OPCode.Text;
            else this.opcode = (byte) SocketFrame.OPCode.Binary;
        }
    }

    // IMPORTANT At the moment, this class does not support sending messages that span multiple frames
    class SocketMessage
    {
        // Start values at value 1 to avoid sending empty application data
        public enum ApplicationMessageCode { Greeting = 1, FetchNOtifications, FetchCurrentArticle, FetchComments }

        // WS standard allows 7 bits to represent message length
        private byte contentLength;
        private ushort appMessageCode;
        private byte  payloadSize;

        public SocketDataFrame.DataFrameType MessageType = SocketDataFrame.DataFrameType.Binary;

        public ushort AppMessageCode
        {
            get { return this.appMessageCode; }
            set {
                this.appMessageCode = value;
                this.payloadSize = MinimumPayloadSize();
            }
        }

        public SocketMessage(ApplicationMessageCode code)
        {
            this.AppMessageCode = (ushort) code;
        }

        private void ComputePayloadLength()
        {
            ushort payload = this.appMessageCode;
            byte MSB = 0;

            // Since we only send numeric values as application messages, the payload length essentially
            // corresponds to the most significant bit of said application message

            while (payload != 0)
            {
                MSB++;
                payload = (ushort)(payload >> 1);
            }

            this.contentLength = MSB;
        }

        /// <summary>
        /// Returns a byte representing the minimum number of butsneeded< to represent the AppMessageCode
        /// </summry>
        private byte MinimumPayloadSize()
        {
            byte minSize = 8;
            if ((ushort) this.appMessageCode >= 256) minSize = 16;

            return minSize;
        }

        // The server currently supports 1 frame messages only
        public List<SocketFrame> GetFrames()
        {
            byte[] payload = BitConverter.GetBytes((int)this.appMessageCode);
            List<SocketFrame> frames = new List<SocketFrame>(1);
            frames.Add(new SocketDataFrame(true, false, this.contentLength, this.MessageType, payload));

            return frames;
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
        0x9 : Ping
        0xA : Pong
 */
