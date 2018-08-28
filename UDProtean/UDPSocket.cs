using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace UDProtean
{
    public abstract class UDPSocket : IDisposable
    {
		internal static double PACKET_LOSS = 0.0;
		Random rand = new Random();

		protected UdpClient socket { get; set; }
		protected CancellationTokenSource runningCancellationToken;

		protected abstract IPEndPoint ReceiveFrom { get; }

		bool socketDisposed = false;

		protected async Task SendDataAsync(byte[] data)
		{
			if (PACKET_LOSS == 0
				|| rand.NextDouble() >= PACKET_LOSS)
			{
				await socket.SendAsync(data, data.Length);
			}
		}

		protected async Task SendDataAsync(byte[] data, IPEndPoint endPoint)
		{
			if (PACKET_LOSS == 0
				|| rand.NextDouble() >= PACKET_LOSS)
			{
				await socket.SendAsync(data, data.Length, endPoint);
			}
		}

		protected void SendMessage(byte[] data)
		{
			SendDataAsync(data).Wait();
		}

		protected void SendMessage(byte[] data, IPEndPoint endPoint)
		{
			SendDataAsync(data, endPoint).Wait();
		}

		protected async Task<UdpReceiveResult> Receive()
		{
			return await socket.ReceiveAsync();
		}

		public void Close()
		{
			if (runningCancellationToken != null && !runningCancellationToken.IsCancellationRequested)
			{
				runningCancellationToken.Cancel();
			}
		}

		public void Dispose()
		{
			Close();
			if (!socketDisposed)
			{
				socketDisposed = true;
				socket.Client.Dispose();
			}
			GC.Collect();
		}

		~UDPSocket()
		{
			Dispose();
		}
	}
}
