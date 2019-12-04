using System;
using System.Collections.Generic;
using System.Security.Cryptography;

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
        public bool Fin;

        /// <summary>
        /// Indicates whether the current WebSocketFrame is masked.
        /// Frames comming from the client must be masked whereas frames sent from the server must NOT be masked
        /// </summary>
        public readonly bool Masked;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public byte[] MaskingKey { get; private set; }

        /// <summary>
        /// Payload of the current WebSocketFrame
        /// </summary>
        protected byte[] _payload;

        /// <summary>
        /// Payload of the current WebSocketFrame
        /// </summary>
        /// <value></value>
        public byte[] Payload
        {
            get { return this._payload; }
            set
            {
                this._payload = value;
            }
        }

		/// <summary>
        /// Length of the payload of the current WebSocketFrame.
		/// Eventhough the WS Protocl specification specifies that a frame may have a payload
		/// length equal to up to the value of an unsigned 64 bits integer, the .NET runtime imposes
		/// a hard limit on the maximum size of any object, that limit being the length of an int
		/// </summary>
        public int PayloadLength { get { return this.Payload.Length; } }

        /// <summary>
        /// The OPCode of the current WebSocketFrame
        /// </summary>
        public byte OpCode;

        /// <summary>
        /// Initializes a new instance of the WebSocketFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current WebSocketFrame is the last one of a multi frame message</param>
        /// <param name="masked">Whether or not the current frame should be masked.true All frames sent by a client should be masked</param>
        /// <param name="payload">Payload of the current WebSocketFrame</param>
        /// <remarks>
        /// If true is passed in as the 'masked' parameter, <see cref="this.MaskingKey" />
        /// will be initialized to a byte array of length 4 which is derived from a secure source of randomness.
        /// Else, <see cref="this.MaskingKey" /> will be initialized to an empty byte array
        /// </remarks>
        public WebSocketFrame(bool fin, bool masked, byte[] payload)
        {
            this.Fin = fin;
            this._payload = payload;
            
            if (masked)
            {
                this.MaskingKey = new byte[4];
                CryptoRandomSingleton.Instance.GetBytes(this.MaskingKey);
            }
            else
            {
                this.MaskingKey = new byte[0];
            }
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
            byte octet0 = (byte) ((this.Fin) ? 0b10000000 : 0b00000000);
            octet0 = (byte) ( octet0 | this.OpCode );

            byte octet1 = (byte) ((this.Masked) ? 0b10000000 : 0b00000000);
            octet1 = (byte) ( octet1 | ( this.PayloadLength <= 125 ? this.PayloadLength : this.PayloadLength <= ushort.MaxValue ? 126 : 127 ) );

            frameHeader[0] = octet0;
            frameHeader[1] = octet1;

            if (this.Payload != null)
            {
				byte[] contentLengthBytes = this.GetContentLengthBytes();
				byte[] headerWithPayloadLength = new byte[frameHeader.Length + contentLengthBytes.Length];
				frameHeader.CopyTo(headerWithPayloadLength, 0);
				contentLengthBytes.CopyTo(headerWithPayloadLength, frameHeader.Length);
                byte[] finalBytes = AppendContentToHeader(headerWithPayloadLength, this.Payload);

                return finalBytes;
            }

            return frameHeader;
        }

		/// <summary>
		/// Returns an array of bytes that represent the value of a given length integer to be inserted in a 
		/// WebSocket frame as the content length.
		/// The returned byte array will be of length:
		/// 0 if the current frame's content length is smaller than or equal to 125
		/// 2 if the current frame's content length is smaller than or equal to 65,535 (ushort)
		/// 4 if the current frame's content length is smaller than or equal to 4,294,967,295 (uint)
		/// </summary>
		/// <returns>An array of bytes that can be inserted in the current frame to represent the content length, if any</returns>
		/// <remark>The length of a frame is a uint hence will never be bigger</remark>
		private byte[] GetContentLengthBytes()
		{
			byte[] contentLengthBytes;
			if (this.PayloadLength <= 125)
			{
				contentLengthBytes = new byte[0];
			}
            // Length must be encoded in network byte order
        	else if (this.PayloadLength <= ushort.MaxValue)
			{
				byte[] encodedLength = BitConverter.GetBytes(this.PayloadLength);
				contentLengthBytes = new byte[2] { encodedLength[1], encodedLength[0] };
			}
			else
			{
				byte[] encodedLength = BitConverter.GetBytes(this.PayloadLength);
				contentLengthBytes = new byte[4] { encodedLength[3], encodedLength[2], encodedLength[1], encodedLength[0] };
			}

			return contentLengthBytes;
		}

        /// <summary>
        /// Appends the payload to the header of the data frame
        /// </summary>
        /// <param name="header">Bytes of the frame header</param>
        /// <param name="message">Bytes of the frame message</param>
        /// <returns>All the bytes of the frame</returns>
        private byte[] AppendContentToHeader(byte[] header, byte[] message)
        {
            byte[] payload = new byte[header.Length + message.Length];
            header.CopyTo(payload, 0);
            message.CopyTo(payload, header.Length);

            return payload;
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

        private string? _plaintext;

        /// <summary>
        /// The plaintext content of the current WebSocketFrame
        /// </summary>
        public string? Plaintext
        {
            get { return this._plaintext; }
            set
            {
                if (value != null)
                {
                    // this.Plaintext = System.Text.Encoding.UTF8.GetString(value);
                    this._plaintext = value;
                    this.Payload = System.Text.Encoding.UTF8.GetBytes(value);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the SocketDataFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current SocketDataFrame is the last one of a multi frame message</param>
        /// <param name="masked">Indicates whether the current SocketDataFrame should be masked or not</param>
        /// <param name="payload">Payload of the current WebSocketFrame</param>
        /// <param name="dataType">The data type of the current SocketDataFrame</param>
        public WebSocketDataFrame(bool fin, bool masked, byte[] payload,
                                DataFrameType dataType) : base(fin, masked, payload)
        {
            this.DataType = dataType;
            this.InitOPCode();
            if (this.DataType == DataFrameType.Text)
            {
                this.Plaintext = System.Text.Encoding.UTF8.GetString(payload);
            }
        }

        /// <summary>
        /// Determines the correct OPCode according to the data type of the current SocketDataFrame
        /// </summary>
        protected override void InitOPCode()
        {
			this.OpCode = (byte) ( this.DataType == DataFrameType.Text ? WebSocketOPCode.Text : WebSocketOPCode.Binary );
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
        public WebSocketControlFrame(WebSocketOPCode controlOpCode) : base(true, false, new byte[0], WebSocketDataFrame.DataFrameType.Binary)
        {
            this.OpCode = (byte) controlOpCode;
        }

        /// <summary>
        /// Initializes a new instance of the SocketControlFrame class
        /// </summary>
        /// <param name="fin"></param>
        /// <param name="masked"></param>
        /// <param name="controlOpCode">The control OPCode of the current SocketControlFrame</param>
        public WebSocketControlFrame(bool fin, bool masked, WebSocketOPCode controlOpCode)
                : base(true, false, new byte[0], WebSocketDataFrame.DataFrameType.Binary)
        {
            this.OpCode = (byte) controlOpCode;
        }
    }

    /// <summary>
    /// Represents a WebSocket Close Control Frame as defined by RFC 6455
    /// </summary>
    public class WebSocketCloseFrame : WebSocketControlFrame
    {
        private WebSocketCloseCode _closeCode;
        private string? _closeReason;

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

                int payloadLength = this._payload.Length > 2 ? this._payload.Length : 2;
                byte[] payload = new byte[payloadLength];

                this._payload.CopyTo(payload, 0);

                byte[] closeCodeBytes = BitConverter.GetBytes((ushort) value);
                closeCodeBytes.CopyTo(payload, 0);

                this.Payload = payload;
            }
        }

        /// <summary>
        /// (Optionnal) indicates a reason for the WebSocket connexion closing.
        /// This text is not necessarily 'human readable' and should ideally not
        /// be show to the end user but may be useful for debugging.
        /// </summary>
        /// <value>The string representing the close reason</value>
        public string? CloseReason
        {
            get { return this._closeReason; }
            set
            {
                this._closeReason = value;

                if (value != null)
                {
                    byte[] payload = new byte[2 + value.Length];
                    byte[] reasonBytes = System.Text.Encoding.UTF8.GetBytes(value);
                    
                    Array.Copy(this._payload, payload, 2);
                    reasonBytes.CopyTo(payload, 2);

                    this.Payload = payload;
                }
            }
        }

        public WebSocketCloseFrame() : this(WebSocketCloseCode.NormalClosure)
        {}

        public WebSocketCloseFrame(WebSocketCloseCode closeCode) : base(WebSocketOPCode.Close)
        {
            // Initialize the first two bytes of the close frame body to a 2-byte unsigned integer
            // as per RFC 6455 section 5.5.1
            this._closeCode = closeCode;
        }

        public WebSocketCloseFrame(WebSocketCloseCode closeCode, string closeReason) : this(closeCode)
        {
            this.CloseReason = closeReason;
        }
    }

    /// <summary>
    /// Represents a message that is to be sent via WebSocket.
    /// A message is composed of one or more frames.
    /// </summary>
    /// <remarks>This public class only support messages that can fit in a single frame for now</remarks>
    public class WebSocketMessage
    {
        /// <summary>
        /// Buffer containing the payload data
        /// </summary>
        public byte[] Payload;

		/// <summary>
		/// Type of the current message
		/// </summary>
		public readonly WebSocketDataFrame.DataFrameType MessageType;

		/// <summary>
		/// Creates an instance of WebSocketMessage that contains the given payload.
		/// <see cref="MessageType" /> will be set to <see cref="WebSocketDataFrame.DataFrameType.Binary" />
		/// </summary>
		/// <param name="payload">The payload to initialize the message with</param>
        public WebSocketMessage(byte[] payload)
        {
			this.MessageType = WebSocketDataFrame.DataFrameType.Binary;
			this.Payload = payload;
        }

		/// <summary>
		/// Creates an instance of WebSocketMessage that contains the given message as payload.
		/// <see cref="MessageType" /> will be set to <see cref="WebSocketDataFrame.DataFrameType.Text" />
		/// </summary>
		/// <param name="message">The message to set as the payload</param>
		public WebSocketMessage(string message)
		{
			this.MessageType = WebSocketDataFrame.DataFrameType.Text;
			this.Payload = System.Text.Encoding.UTF8.GetBytes(message);
		}

        /// <summary>
        /// Returns the Websocket frames that compose the current message, as per
        /// the websocket standard
        /// </summary>
        /// <remarks>The method currently supports only 1 frame messages</remarks>
        /// <returns>A List containing the frames of the message</returns>
        public List<WebSocketDataFrame> GetFrames()
        {
            List<WebSocketDataFrame> frames = new List<WebSocketDataFrame>(1);
            frames.Add(new WebSocketDataFrame(true, false, this.Payload, this.MessageType));

            return frames;
        }

        public List<WebSocketDataFrame> GetMaskedFrames()
        {
            List<WebSocketDataFrame> frames = new List<WebSocketDataFrame>(1);
            frames.Add(new WebSocketDataFrame(true, true, this.Payload, this.MessageType));

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
        0x8 : Close
        0x9 : Ping
        0xA : Pong
 */
