using System;

namespace NarcityMedia.Enjent
{
	
	/// <summary>
	/// Represents types of WebSocket payload data
	/// </summary>
	public enum WebSocketDataType { Text, Binary }

	public abstract class WebSocketPayload
	{
		public readonly WebSocketDataType DataType;

        /// <summary>
        /// Gets or sets the payload current <see cref="WebSocketPayload" />
        /// </summary>
        /// <value>Binary representation of the data</value>
        public byte[] Payload;

		public WebSocketPayload(byte[] payload)
		{
			this.Payload = payload;
		}
	}

	public class BinaryPayload : WebSocketPayload
	{

		/// <summary>
		/// Represents the type of the current <see cref="BinaryPayload" /> which is <see cref="WebSocketDataType.Binary" />
		/// </summary>
		public new readonly WebSocketDataType DataType = WebSocketDataType.Binary;

		public BinaryPayload() : this(new byte[0])
		{}

		public BinaryPayload(byte[] payload) : base(payload)
		{}
	}

	public class TextPayload : BinaryPayload
	{
		
		/// <summary>
		/// Represents the type of the current <see cref="BinaryPayload" /> which is <see cref="WebSocketDataType.Binary" />
		/// </summary>
		public new readonly WebSocketDataType DataType = WebSocketDataType.Text;

		private string _plaintext;

		public string Plaintext
		{
			get { return this._plaintext; }
			set
			{
				this._plaintext = value;
				this.Payload = System.Text.Encoding.UTF8.GetBytes(value);
			}
		}

		public TextPayload(string plaintext)
		{
			this._plaintext = String.Empty;
			this.Payload = System.Text.Encoding.UTF8.GetBytes(plaintext);
		}
	}
}
