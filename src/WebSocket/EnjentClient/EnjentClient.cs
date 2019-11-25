using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NarcityMedia.Enjent.Client
{

	public class EnjentClient
	{
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
		private Socket Socket;

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

		public async Task Connect()
		{
			if (this.Endpoint == null)
			{
				string unescapedHost = Uri.UnescapeDataString(this.ServerUri.IdnHost);
				IPAddress[] ipAddresses = await Dns.GetHostAddressesAsync(unescapedHost);
				if (ipAddresses.Length > 0)
				{
					this.Endpoint = new IPEndPoint(ipAddresses[0], this.ServerUri.Port);
					Socket s = new Socket(this.Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					await s.ConnectAsync(this.Socket, ipAddresses);
				}
				else
				{
					throw new Exception("DNS resolution failed for hostname" + unescapedHost);
				}
			}
		}
	}
}
