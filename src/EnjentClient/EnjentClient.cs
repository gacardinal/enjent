using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NarcityMedia.Enjent.Client
{

	public class EnjentClient
	{
		private static int MAX_HTTP_RES_LENGTH = 2048;

		/// <summary>
		/// Indicates whether this EnjentClient instance currently has a connection open with a WebSocket server
		/// </summary>
		/// <value>Whether the current EnjentClient is connected or not</value>
		public Boolean Connected { get; private set; }

		/// <summary>
		/// The address at which the current client will connect
		/// </summary>
		public readonly Uri ServerUri;

		/// <summary>
		/// IP endpoint at which the current client connects
		/// </summary>
		private IPEndPoint? Endpoint;

		/// <summary>
		/// Socket used to communicate between the current client and server
		/// </summary>
		private Socket? Socket;

		/// <summary>
		/// Initializes a new instance of EnjentClient
		/// </summary>
		/// <param name="serverUri">
		/// The Uri where the current client should attempt to connect
		/// when <see cref="EnjentClient.Connect" /> is called
		/// </param>
		public EnjentClient(Uri serverUri)
		{
			if (!serverUri.IsAbsoluteUri)
				throw new ArgumentException("Must be an absolute Uri", "serverUri");
			UriHostNameType hnt = Uri.CheckHostName(serverUri.IdnHost);
			if (hnt == UriHostNameType.Unknown)
				throw new ArgumentException("URI host must be set", "serverUri.Host");
			if (serverUri.Scheme != "ws")
				throw new NotSupportedException("This class only supports the ws protocol fo rnow");

			this.ServerUri = serverUri;
		}

		private static byte[] GetHttpRequestBytes()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("GET /chat HTTP/1.1");
			sb.AppendLine("Host: server.example.com");
			sb.AppendLine("Upgrade: websocket");
			sb.AppendLine("Connection: Upgrade");
			sb.AppendLine("Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==");
			sb.AppendLine("Origin: http://example.com");
			sb.AppendLine("Sec-WebSocket-Protocol: chat, superchat");
			sb.AppendLine("Sec-WebSocket-Version: 13");
			sb.AppendLine("");

			return Encoding.UTF8.GetBytes(sb.ToString());
		}

		public async Task Connect()
		{
			if (this.Connected)
			{
				throw new InvalidOperationException("Cannot connect to a remote endpoint while current endpoint is already connected");
			}

			if (this.Endpoint == null)
			{
				string unescapedHost = Uri.UnescapeDataString(this.ServerUri.IdnHost);
				IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(unescapedHost);
				if (ipAddresses.Length > 0)
				{
					this.Endpoint = new IPEndPoint(ipAddresses[0], this.ServerUri.Port);
					this.Socket = new Socket(this.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				}
				else
				{
					throw new Exception("DNS resolution failed for hostname " + unescapedHost);
				}
			}

			try
			{
				await this.Socket.ConnectAsync(this.Endpoint);
				await this.Socket.SendAsync(EnjentClient.GetHttpRequestBytes(), SocketFlags.None);
				this.Connected = true;

				byte[] buf = new byte[MAX_HTTP_RES_LENGTH];
				NetworkStream s = new NetworkStream(this.Socket);
				int read = await s.ReadAsync(buf);
				string message = Encoding.UTF8.GetString(buf);
				
			}
			catch (Exception e)
			{
				throw e;
			}
		}


		public void Send(WebSocketMessage<WebSocketTextFrame> binMessage)
		{
			this.Send(binMessage.GetFrames());
		}

		public void Send(WebSocketMessage<WebSocketBinaryFrame> binMessage)
		{
			this.Send(binMessage.GetFrames());
		}

		public void Send(IEnumerable<WebSocketFrame> frames)
		{
			if (this.Socket != null && this.Connected)
			{
				foreach (WebSocketFrame f in frames)
				{
					this.Socket.Send(f.GetBytes());
				}
			}
			else
			{
				throw new InvalidOperationException("Cannot send data while the client is not connected");
			}
		}
	}
}
