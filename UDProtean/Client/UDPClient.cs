using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using UDProtean.Events;
using System.Threading;

namespace UDProtean.Client
{
    public class UDPClient : UDPSocket
    {
		IPEndPoint serverEndPoint;

		SequentialCommunication comm;

		public event EventHandler<OpenEventArgs> OnOpen;
		public event EventHandler<MessageEventArgs> OnMessage;
		public event EventHandler<ErrorEventArgs> OnError;
		public event EventHandler<CloseEventArgs> OnClose;
		public event EventHandler<LogEventArgs> OnLog;

		protected override IPEndPoint ReceiveFrom => serverEndPoint;

		public UDPClient(string host, int port) : base()
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
			socket.ExclusiveAddressUse = false;
			socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			comm = new SequentialCommunication(
				new SendData(SendData),
				new DataCallback(OnReceivedData)
			);
		}

		public void Connect()
		{
			runningCancellationToken = new CancellationTokenSource();

			socket.Connect(serverEndPoint);

			Task.Factory.StartNew(() => Listener(runningCancellationToken.Token));

			SendData(new byte[4]);
		}

		public void Send(byte[] data)
		{
			try
			{
				comm.Send(data);
			}
			catch (Exception ex)
			{
				OnError?.Invoke(this, new ErrorEventArgs(ex));
			}
		}

		public void Send(string message)
		{
			Send(message, default(Encoding));
		}

		public void Send(string message, Encoding encoding)
		{
			Send(encoding.GetBytes(message));
		}

		#region private

		async Task Listener(CancellationToken cancellationToken = default(CancellationToken))
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				byte[] dgram = await ReceiveFromServer(cancellationToken);

				comm.Received(dgram);

				comm.Flush();
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
			SendMessage(data);
		}

		void OnReceivedData(byte[] data)
		{
			OnMessage?.Invoke(this, new MessageEventArgs(data));
		}

		#endregion
	}
}
