using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using UDProtean.Shared;
using UDProtean.Events;
using System.Threading;

namespace UDProtean.Client
{
    public class UDPClient
    {
		IPEndPoint serverEndPoint;
		UdpClient socket;

		CancellationTokenSource runningCancellationToken;

		SequentialCommunication comm;

		public event EventHandler<OpenEventArgs> OnOpen;
		public event EventHandler<DataEventArgs> OnData;
		public event EventHandler<ErrorEventArgs> OnError;
		public event EventHandler<CloseEventArgs> OnClose;
		public event EventHandler<LogEventArgs> OnLog;

		public UDPClient(string host, int port)
		{
			IPAddress serverAddress;

			if (!IPAddress.TryParse(host, out serverAddress))
			{
#if DNS_AVAILABLE
				serverAddress = Dns.GetHostAddresses(host)[0];
#else
				throw new ArgumentException("Invalid IP addres: " + host);
#endif
			}

			serverEndPoint = new IPEndPoint(serverAddress, port);

			socket = new UdpClient();

			comm = new SequentialCommunication(
				new SendData(SendData),
				new DataCallback(OnReceivedData)
				);
		}

		public void Connect()
		{
			runningCancellationToken = new CancellationTokenSource();
			Task.Factory.StartNew(() => Listener(runningCancellationToken.Token));

			SendData(new byte[4]);
		}

		public void Close()
		{
			runningCancellationToken.Cancel();
		}

		public void Send(byte[] data)
		{
			comm.Send(data);
		}

		async Task Listener(CancellationToken cancellationToken = default(CancellationToken))
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				byte[] dgram = await Receive();

				comm.Received(dgram);
			}
		}

		async Task<byte[]> Receive(CancellationToken cancellationToken = default(CancellationToken))
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					UdpReceiveResult dgram = await socket.ReceiveAsync();

					if (dgram.RemoteEndPoint.Equals(serverEndPoint))
					{
						return dgram.Buffer;
					}
				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, new ErrorEventArgs(ex));
				}
			}
			return null;
		}

		async Task SendDataAsync(byte[] data)
		{
			await socket.SendAsync(data, data.Length, serverEndPoint);
		}

		void SendData(byte[] data)
		{
			SendDataAsync(data).Wait();
		}

		void OnReceivedData(byte[] data)
		{
			OnData?.Invoke(this, new DataEventArgs(data));
		}
	}
}
