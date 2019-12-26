using System;
using System.Text;

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
        public readonly bool Fin;

        /// <summary>
        /// Indicates whether the current WebSocketFrame is masked.
        /// Frames comming from the client must be masked whereas frames sent from the server must NOT be masked
        /// </summary>
        public readonly bool Masked;

        /// <summary>
        /// The OPCode of the current WebSocketFrame
        /// </summary>
        public byte OpCode;

		public byte[] Payload;

		public BinaryPayload P;

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public byte[] MaskingKey { get; private set; }

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
			this.Payload = payload;
			this.P = new BinaryPayload(payload);
            
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
        /// Returns the bytes representation of the data frame
        /// </summary>
        /// <returns>Returns the bytes that form the data frame</returns>
        public byte[] GetBytes()
        {
			// It's useful to determine the exact length of the frame beforehand
			// to avoid some potential memory re-allocation
			byte[] frameBytes = new byte[this.PredictFrameLength()];

            // First octet has 1 bit for FIN, 3 reserved, 4 for OP Code
            byte octet0 = (byte) ((this.Fin) ? 0b10000000 : 0b00000000);
            octet0 = (byte) ( octet0 | this.OpCode );

            byte octet1 = (byte) ((this.Masked) ? 0b10000000 : 0b00000000);
            octet1 = (byte) ( octet1 | ( this.Payload.Length <= 125 ? this.Payload.Length : this.Payload.Length <= ushort.MaxValue ? 126 : 127 ) );

            frameBytes[0] = octet0;
            frameBytes[1] = octet1;

			byte[] contentLengthBytes = this.GetContentLengthBytes();
			contentLengthBytes.CopyTo(frameBytes, 2);
			
			if (this.Masked && this.Payload.Length > 0)
			{
				this.MaskingKey.CopyTo(frameBytes, 2 + contentLengthBytes.Length);
			}

			int payloadWriteStart = 2 + contentLengthBytes.Length + ( this.Masked ? 4 : 0 );
			this.Payload.CopyTo(frameBytes, payloadWriteStart);

            return frameBytes;
        }

		/// <summary>
		/// Returns the number of bytes necessary to represent the current
		/// frame as a byte array
		/// </summary>
		/// <returns>
		/// The number of bytes
		/// </returns>
		protected int PredictFrameLength()
		{
			return (
				2
				+ ( this.Payload.Length <= 125 ? 0 : this.Payload.Length <= ushort.MaxValue ? 2 : 4 )
				+ ( this.Masked ? 4 : 0 )
				+ this.Payload.Length
			);
		}

		/// <summary>
		/// Returns an array of bytes that represent the length of to be inserted in a 
		/// WebSocket frame as the content length.
		/// The returned byte array will be of length:
		/// 0 if the current frame's content length is smaller than or equal to 125
		/// 2 if the current frame's content length is smaller than or equal to 65,535 (ushort)
		/// 4 if the current frame's content length is smaller than or equal to 4,294,967,295 (uint)
		/// </summary>
		/// <returns>An array of bytes that can be inserted in the current frame to represent the content length, if any</returns>
		/// <remark>The length of a frame is a uint hence will never be bigger</remark>
		protected byte[] GetContentLengthBytes()
		{
			byte[] contentLengthBytes;
			if (this.Payload.Length <= 125)
			{
				contentLengthBytes = new byte[0];
			}
            // Length must be encoded in network byte order
        	else if (this.Payload.Length <= ushort.MaxValue)
			{
				byte[] encodedLength = BitConverter.GetBytes(this.Payload.Length);
				contentLengthBytes = new byte[2] { encodedLength[1], encodedLength[0] };
			}
			else
			{
				byte[] encodedLength = BitConverter.GetBytes(this.Payload.Length);
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


	public abstract class WebSocketDataFrame : WebSocketFrame
	{
		public readonly WebSocketDataType DataType;

		public WebSocketDataFrame(bool fin, bool masked, byte[] payload, WebSocketDataType dataType) : base(fin, masked, payload)
		{
			this.DataType = dataType;
		}
	}

    /// <summary>
    /// Represents a WebSocket frame that contains binary data
    /// </summary>
    public class WebSocketBinaryFrame : WebSocketDataFrame
    {
		public WebSocketBinaryFrame(byte[] payload) : this(true, false, payload)
		{}

        /// <summary>
        /// Initializes a new instance of the SocketDataFrame class
        /// </summary>
        /// <param name="fin">Indicates whether the current SocketDataFrame is the last one of a multi frame message</param>
        /// <param name="masked">Indicates whether the current SocketDataFrame should be masked or not</param>
        /// <param name="payload">Payload of the current WebSocketFrame</param>
        /// <param name="dataType">The data type of the current SocketDataFrame</param>
        public WebSocketBinaryFrame(bool fin, bool masked, byte[] payload) : base(fin, masked, payload, WebSocketDataType.Binary)
        {}
    }

	public class WebSocketTextFrame : WebSocketDataFrame
	{
		new public TextPayload P;


		public WebSocketTextFrame(string plaintext) : this(true, false, plaintext)
		{}

		public WebSocketTextFrame(bool fin, bool masked, string plaintext) : base(fin, masked, new byte[0], WebSocketDataType.Text)
		{
			this.P = new TextPayload(plaintext);
		}
	}

    /// <summary>
    /// Represents a WebSocket control frame as described in RFC 6455
    /// </summary>
    public abstract class WebSocketControlFrame : WebSocketFrame
    {
        /// <summary>
        /// Initializes a new instance of the SocketControlFrame class
        /// </summary>
        /// <param name="controlOpCode">The control OPCode of the current SocketControlFrame</param>
        public WebSocketControlFrame(WebSocketOPCode controlOpCode) : this(controlOpCode, false, new byte[0])
        {}

		/// <summary>
		/// Initializes a new instance of the SocketControlFrame class
		/// </summary>
		/// <param name="controlOpCode">The control OPCode of the current SocketControlFrame</param>
		/// <param name="masked">Whether or not the current control frame is masked</param>
		public WebSocketControlFrame(WebSocketOPCode controlOpCode, bool masked, byte[] payload) : base(true, masked, payload)
		{}
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

                int payloadLength = this.Payload.Length > 2 ? this.Payload.Length : 2;
                byte[] payload = new byte[payloadLength];

                this.Payload.CopyTo(payload, 0);

                byte[] closeCodeBytes = BitConverter.GetBytes((ushort) value);
				closeCodeBytes.EnsureNetworkByteOrder();
                closeCodeBytes.CopyTo(payload, 0);

                this.Payload = payload;
            }
        }

        /// <summary>
        /// (Optionnal) indicates a reason for the WebSocket connexion closing.
        /// This text is not necessarily 'human readable' and should ideally not
        /// be shown to the end user but may be useful for debugging.
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
                    
                    Array.Copy(this.Payload, payload, 2);
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

	public class WebSocketPingFrame : WebSocketControlFrame
	{
		public WebSocketPingFrame() : base(WebSocketOPCode.Ping)
		{}

		public WebSocketPingFrame(bool masked, byte[] payload) : base(WebSocketOPCode.Ping, masked, payload)
		{}
	}

	public class WebSocketPongFrame : WebSocketControlFrame
	{
		public WebSocketPongFrame() : base(WebSocketOPCode.Pong)
		{}
		
		public WebSocketPongFrame(bool masked, byte[] payload) : base(WebSocketOPCode.Ping, masked, payload)
		{}
	}

	internal static class NetworkByteOrderByteArrayExtensions
	{
		public static void EnsureNetworkByteOrder(this byte[] that)
		{
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(that);
			}
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
