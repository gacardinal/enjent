using System;

namespace NarcityMedia.Enjent
{
	
	/// <summary>
	/// Represents types of WebSocket payload data
	/// </summary>
	public enum WebSocketDataType { Text, Binary }


	public abstract class WebSocketDataContainer
	{
		/// <summary>
		/// UTF8 string data of the current <see cref="WebSocketDataContainer" />.
		/// Null if <see cref="WebSocketDataContainer.DataType" /> is not <see cref="WebSocketDataType.Text" />
		/// </summary>
        private string? _plaintext;

        /// <summary>
        /// Payload of the current <see cref="WebSocketDataContainer" />
        /// </summary>
        protected byte[] _payload;

        /// <summary>
        /// Gets or sets the payload current <see cref="WebSocketDataContainer" />
        /// </summary>
        /// <value>Binary representation of the data</value>
        public byte[] Payload
        {
            get { return this._payload; }
            set
            {
				if (this.DataType == WebSocketDataType.Text)
				{
					try
					{
						string newText = System.Text.Encoding.UTF8.GetString(value);
						this._plaintext = newText;
						this._payload = value;
					}
					catch (Exception e)
					{
						throw new EnjentWebSocketProtocolException("Tried to set invalid UTF8 data as payload", e);
					}
				}
				else
				{
                	this._payload = value;
				}
            }
        }

		/// <summary>
		/// Type of the current message
		/// </summary>
		public readonly WebSocketDataType DataType;

		public WebSocketDataContainer(byte[]? payload)
		{
			this._payload = payload ?? new byte[0];
			this.DataType = WebSocketDataType.Binary;
		}

		public WebSocketDataContainer(string? plaintext) {
			this._plaintext = plaintext ?? String.Empty;
			this._payload = System.Text.Encoding.UTF8.GetBytes(plaintext);
		}

		/// <summary>
		/// Gets the data of the current <see cref="WebSocketDataContainer" /> as a string
		/// </summary>
		/// <returns>Plaintext representation of the data</returns>
		/// <remarks>
		/// If <see cref="DataType" /> is set to <see cref="WebSocketDataType.Binary" />, this method returns the binary data
		/// as a Base64 encoded string
		/// </remarks>
		/// 
		public string GetPlaintext()
		{
			if (this.DataType == WebSocketDataType.Text)
			{
				return this._plaintext ?? String.Empty;
			}
			else
			{
				return Convert.ToBase64String(this._payload);
			}
		}
	}
}
