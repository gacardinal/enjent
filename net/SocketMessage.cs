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
        }

        protected abstract void InitOPCode();

        public byte[] GetBytes()
        {
            byte[] frame = new byte[3];

            // First octet - 1 bit for FIN, 3 reserved, 4 for OP Code
            byte octet0 = (byte) ((this.fin) ? 0b10000000 : 0b00000000);
            octet0 = (byte) (octet0 | this.opcode);

            byte octet1 = (byte) ((this.masked) ? 0b10000000 : 0b00000000);
            // TODO: COmpute content length instead of hardcoding it
            octet1 = (byte) (octet1 | 0b00001000);

            byte octet2 = 0b00000001;

            Console.WriteLine("OCTET 0 : " + Convert.ToString(octet0, 2) + " " + Convert.ToString(octet1, 2) + " " + Convert.ToString(octet2, 2));

            frame[0] = octet0;
            frame[1] = octet1;
            frame[2] = octet2;

            return frame;
        }
    }

    class SocketDataFrame : SocketFrame
    {
        public enum DataFrameType { Text, Binary }
        public DataFrameType DataType;

        // Codes must match positions in the ControlFrameTypes enum
        private readonly byte[] DataFrameTypesOPCodes = { 0x1, 0x2 };

        public SocketDataFrame(bool fin, bool masked, byte length, DataFrameType dataType) : base(fin, masked, length)
        {
            this.DataType = dataType;
            this.InitOPCode();
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

        public SocketDataFrame.DataFrameType MessageType;

        public ushort AppMessageCode
        {
            get { return this.appMessageCode; }
            set {
                this.appMessageCode = value;
                ComputePayloadLength();
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

        // The server currently supports 1 frame messages only
        public List<SocketFrame> GetFrames()
        {
            List<SocketFrame> frames = new List<SocketFrame>(1);
            frames.Add(new SocketDataFrame(true, false, this.contentLength, SocketDataFrame.DataFrameType.Binary));

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
