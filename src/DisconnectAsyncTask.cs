using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NarcityMedia.Enjent
{
	internal static class SocketDisconnectAsyncTaskExtension
	{
		internal static Task<SocketAsyncEventArgs> DisconnectAsync(this Socket that)
		{
			TaskCompletionSource<SocketAsyncEventArgs> tcs = new TaskCompletionSource<SocketAsyncEventArgs>();
			SocketAsyncEventArgs SAE = new SocketAsyncEventArgs();
			SAE.Completed += (object sender, SocketAsyncEventArgs args) => {
				tcs.SetResult(args);
			};

			bool isIOPending = that.DisconnectAsync(SAE);
			if (isIOPending)
			{
				tcs.SetResult(SAE);
			}

			return tcs.Task;
		}
	}
}