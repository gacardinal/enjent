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
		public readonly IPEndPoint? Endpoint;

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

				}
				else
				{
					throw new Exception("DNS resolution failed for hostname" + unescapedHost);
				}
			}
		}
	}
}
