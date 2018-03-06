using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace UDProtean.Shared
{
    public abstract class UDPSocket
    {
		internal static double PACKET_LOSS = 0.0;
		Random rand = new Random();

		protected UdpClient socket { get; set; }

		protected async Task SendDataAsync(byte[] data, IPEndPoint endPoint)
		{
			if (rand.NextDouble() >= PACKET_LOSS)
			{
				await socket.SendAsync(data, data.Length, endPoint);
			}
		}

		protected void SendData(byte[] data, IPEndPoint endPoint)
		{
			SendDataAsync(data, endPoint).Wait();
		}

		protected async Task<UdpReceiveResult> Receive()
		{
			return await socket.ReceiveAsync();
		}

		protected void Dispose()
		{
			(socket as IDisposable).Dispose();
		}
	}
}
