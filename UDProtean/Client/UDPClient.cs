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
    public class UDPClient : UDPSocket
    {
		IPEndPoint serverEndPoint;

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

		public void Send(byte[] data)
		{
			comm.Send(data);
		}

		public void Send(string message)
		{
			Send(message, default(Encoding));
		}

		public void Send(string message, Encoding encoding)
		{
			comm.Send(encoding.GetBytes(message));
		}

		async Task Listener(CancellationToken cancellationToken = default(CancellationToken))
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				byte[] dgram = await ReceiveFromServer();

				comm.Received(dgram);
			}
		}

		async Task<byte[]> ReceiveFromServer(CancellationToken cancellationToken = default(CancellationToken))
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					UdpReceiveResult dgram = await Receive();

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

		void SendData(byte[] data)
		{
			SendData(data, serverEndPoint);
		}

		void OnReceivedData(byte[] data)
		{
			OnData?.Invoke(this, new DataEventArgs(data));
		}
	}
}
