using System;
using System.Numerics;
using System.Collections.Generic;

namespace NarcityMedia.Enjent
{
    /// <summary>
    /// Enumerates the possible OPCodes of a WebSocket frame as described in RFC 6455
    /// </summary>
    public enum WebSocketOPCode
    {
        /// <summary>
        /// Represents the continuation of a message that was sent over two WebSocket frames
        /// </summary>
        Continuation = 0x0,

        /// <summary>
        /// Indicates a text payload
        /// </summary>
        Text = 0x1,

        /// <summary>
        /// Indicates a binary payload
        /// </summary>
        Binary = 0x2,

        /// <summary>
        /// Indicates a closing frame
        /// </summary>
        Close = 0x8,

        /// <summary>
        /// Indicates a PING frame
        /// </summary>
        Ping = 0x9,

        /// <summary>
        /// Indicates a PONG frame
        /// </summary>
        Pong = 0xA
    }

    /// <summary>
    /// Represents WebSocket close codes as defined by the RFC 6455 specification
    /// </summary>
    public enum WebSocketCloseCode
    {
        /// <summary>
        /// Indicates a normal closure, meaning that the purpose for
        /// which the connection was established has been fulfilled.
        /// </summary>
        NormalClosure = 1000,

        /// <summary>
        /// Indicates that an endpoint is "going away", such as a server
        /// going down or a browser having navigated away from a page.
        /// </summary>
        GoingAway = 1001,
    
        /// <summary>
        /// Indicates that an endpoint is terminating the connection due
        /// to a protocol error.
        /// </summary>
        ProtocolError = 1002,

        /// <summary>
        /// Indicates that an endpoint is terminating the connection
        /// because it has received a type of data it cannot accept (e.g., an
        /// endpoint that understands only text data MAY send this if it
        /// receives a binary message).
        /// </summary>
        UnacceptableDataType = 1003,

        // 1004 is undefined

        /// <summary>
        /// Is a reserved value and MUST NOT be set as a status code in a
        /// Close control frame by an endpoint.  It is designated for use in
        /// applications expecting a status code to indicate that no status
        /// code was actually present.
        /// </summary>
        NoCloseCode = 1005,

        /// <summary>
        /// is a reserved value and MUST NOT be set as a status code in a
        /// Close control frame by an endpoint.  It is designated for use in
        /// applications expecting a status code to indicate that the
        /// connection was closed abnormally, e.g., without sending or
        /// receiving a Close control frame.
        /// </summary>
        AbnormalClose = 1006,

        /// <ummary>
        /// Indicates that an endpoint is terminating the connection
        /// because it has received data within a message that was not
        /// consistent with the type of the message (e.g., non-UTF-8 [RFC3629]
        /// data within a text message).
        /// </summary>
        InconsistentDataType = 1007,

        /// <summary>
        /// Indicates that an endpoint is terminating the connection
        /// because it has received a message that violates its policy.  This
        /// is a generic status code that can be returned when there is no
        /// other more suitable status code (e.g., 1003 or 1009) or if there
        /// is a need to hide specific details about the policy.
        /// </summary>
        PolicyViolation = 1008,

        /// <summary>
        /// Indicates that an endpoint is terminating the connection
        /// because it has received a message that is too big for it to
        /// process.
        /// </summary>
        MessageSizeExceeded = 1009,

        /// <summary>
        /// dicates that an endpoint (client) is terminating the
        /// connection because it has expected the server to negotiate one or
        /// more extension, but the server didn't return them in the response
        /// message of the WebSocket handshake.  The list of extensions that
        /// are needed SHOULD appear in the /reason/ part of the Close frame.
        /// Note that this status code is not used by the server, because it
        /// can fail the WebSocket handshake instead.
        /// </summary>
        ExtensionNegotiationFailure = 1010,

        /// <summary>
        /// ndicates that a server is terminating the connection because
        /// it encountered an unexpected condition that prevented it from
        /// fulfilling the request.
        /// </summary>
        UnexpectedCondition = 1011,

        // 1012 is undefined
        // 1013 is undefined
        // 1014 is undefined

        /// <summary>
        /// is a reserved value and MUST NOT be set as a status code in a
        /// Close control frame by an endpoint.  It is designated for use in
        /// applications expecting a status code to indicate that the
        /// connection was closed due to a failure to perform a TLS handshake
        /// (e.g., the server certificate can't be verified).
        /// </summary>
        TLSHandshakeFailure = 1015
    }

    /// <summary>
    /// Represents a general concept of a WebSocket frame described by the 
    /// WebSocket standard
    /// </summary>
    public abstract partial class WebSocketFrame
    {
        /// <summary>
        /// Indicates whether the current WebSocketFrame is the last one of a message
        /// </summary>
        public bool fin;

        /// <summary>
        /// Indicates whether the current WebSocketFrame is masked.
        ///  Frames comming from the client must be masked whereas frames sent from the server must NOT be masked
        /// </summary>
        protected bool masked;

        /// <summary>
        /// Payload of the current WebSocketFrame
        /// </summary>
        protected byte[] _data;

        /// <summary>
        /// Payload of the current WebSocketFrame
        /// </summary>
        /// <value></value>
        public byte[] data
        {
            get { return this._data; }
            set
            {
                this._data = value;
                this.contentLength = value.Length;
                this.Plaintext = System.Text.Encoding.UTF8.GetString(value);
            }
        }

        /// <summary>
        /// Length of the payload of the current WebSocketFrame
        /// </summary>
        public int contentLength;

        /// <summary>
        /// The OPCode of the current WebSocketFrame
        /// </summary>
        public byte opcode;

        /// <summary>
        /// The plaintext content of the current WebSocketFrame
        /// </summary>
        public string Plaintext;

        /// <summary>
        /// Initializes a new instance of the WebSocketFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current WebSocketFrame is the last one of a multi frame message</param>
        /// <param name="masked">Indicates whether the current WebSocketFrame is masked</param>
        /// <param name="length">Length of the content of the current WebSocketFrame</param>
        public WebSocketFrame(bool fin, bool masked, int length)
        {
            this.fin = fin;
            this.masked = masked;
            this.contentLength = length;
            this._data = new byte[length];
        }

        /// <summary>
        /// Initializes the OPCode of the frame, the frame must decide it's own opcode
        /// </summary>
        /// <remarks>Derived public classes MUST override</remarks>
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
    public class WebSocketDataFrame : WebSocketFrame
    {
        /// <summary>
        /// Represents types of WebSocket data frames
        /// </summary>
        public enum DataFrameType { Text, Binary }

        /// <summary>
        /// Represents the type of the current SocketDataFrame
        /// </summary>
        public DataFrameType DataType;

        /// <summary>
        /// Initializes a new instance of the SocketDataFrame class
        /// </summary>
        public WebSocketDataFrame() : base(true, true, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the SocketDataFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current SocketDataFrame is the last one of a multi frame message</param>
        /// <param name="masked">Indicates whether the current SocketDataFrame is masked</param>
        /// <param name="length">Length of the content of the current SocketDataFrame</param>
        /// <param name="dataType">The data type of the current SocketDataFrame</param>
        public WebSocketDataFrame(bool fin, bool masked, ushort length,
                                DataFrameType dataType) : base(fin, masked, length)
        {
            this.DataType = dataType;
            this.InitOPCode();
        }

        /// <summary>
        /// Initializes a new instance of the SocketDataFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current SocketDataFrame is the last one of a multi frame message</param>
        /// <param name="masked">Indicates whether the current SocketDataFrame is masked</param>
        /// <param name="length">Length of the content of the current SocketDataFrame</param>
        /// <param name="dataType">The data type of the current SocketDataFrame</param>
        /// <param name="message">Payload of the current SocketDataFrame</param>
        /// <exception cref="ArgumentOutOfRangeException">If the length of the message is greater than 65536</exception>
        /// <remark>This class, for now, only supports messages of length smaller of equal to 65536, that is, messages that can fit in a single frame</remark>
        public WebSocketDataFrame(bool fin, bool masked, ushort length,
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

        /// <summary>
        /// Determines the correct OPCode according to the data type of the current SocketDataFrame
        /// </summary>
        protected override void InitOPCode()
        {
            if (this.DataType == DataFrameType.Text) this.opcode = (byte) WebSocketOPCode.Text;
            else this.opcode = (byte) WebSocketOPCode.Binary;
        }
    }

    /// <summary>
    /// Represents a WebSocket control frame as described in RFC 6455
    /// </summary>
    public class WebSocketControlFrame : WebSocketDataFrame
    {
        /// <summary>
        /// Initializes a new instance of the SocketControlFrame class
        /// </summary>
        /// <param name="controlOpCode">The control OPCode of the current SocketControlFrame</param>
        public WebSocketControlFrame(WebSocketOPCode controlOpCode) : base(true, false, 0,WebSocketDataFrame.DataFrameType.Binary)
        {
            this.opcode = (byte) controlOpCode;
        }

        /// <summary>
        /// Initializes a new instance of the SocketControlFrame class
        /// </summary>
        /// <param name="fin"></param>
        /// <param name="masked"></param>
        /// <param name="controlOpCode">The control OPCode of the current SocketControlFrame</param>
        public WebSocketControlFrame(bool fin, bool masked, WebSocketOPCode controlOpCode)
                : base(true, false, 0, WebSocketDataFrame.DataFrameType.Binary)
        {
            this.opcode = (byte) controlOpCode;
        }
    }

    /// <summary>
    /// Represents a WebSocket Close Control Frame as defined by RFC 6455
    /// </summary>
    public class WebSocketCloseFrame : WebSocketControlFrame
    {
        private WebSocketCloseCode _closeCode;
        private string _closeReason;

        /// <summary>
        /// Represents the <see cref="WebSocketCLoseCode"> for the closing of a WebSocket connection.
        /// </summary>
        /// <value>The <see cref="WebSocketCLoseCode"> for the closing of a WebSocket connection</value>
        public WebSocketCloseCode CloseCode
        {
            get { return this._closeCode; }
            set
            {
                this._closeCode = value;

                int payloadLength = this._data.Length > 2 ? this._data.Length : 2;
                byte[] payload = new byte[payloadLength];

                this._data.CopyTo(payload, 0);

                byte[] closeCodeBytes = BitConverter.GetBytes((ushort) value);
                closeCodeBytes.CopyTo(payload, 0);

                this.data = payload;
            }
        }

        /// <summary>
        /// (Optionnal) indicates a reason for the WebSocket connexion closing.
        /// This text is not necessarily 'human readable' and should ideally not
        /// be show to the end user but may be useful for debugging.
        /// </summary>
        /// <value>The string representing the close reason</value>
        public string CloseReason
        {
            get { return this._closeReason; }
            set
            {
                this._closeReason = value;

                byte[] payload = new byte[2 + value.Length];
                byte[] reasonBytes = System.Text.Encoding.UTF8.GetBytes(value);
                
                Array.Copy(this._data, payload, 2);
                reasonBytes.CopyTo(payload, 2);

                this.data = payload;
            }
        }

        public WebSocketCloseFrame() : this(WebSocketCloseCode.NormalClosure)
        {}

        public WebSocketCloseFrame(WebSocketCloseCode closeCode) : base(WebSocketOPCode.Close)
        {
            // Initialize the first two bytes of the close frame body to a 2-byte unsigned integer
            // as per RFC 6455 section 5.5.1
            this.CloseCode = closeCode;
        }

        public WebSocketCloseFrame(WebSocketCloseCode closeCode, string closeReason) : this(closeCode)
        {
            this.CloseReason = closeReason;
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
