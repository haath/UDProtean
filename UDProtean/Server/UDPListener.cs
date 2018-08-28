using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UDProtean.Events;

namespace UDProtean.Server
{
    public abstract class UDPListener : UDPSocket
    {
		IPEndPoint endPoint;

		protected override IPEndPoint ReceiveFrom => endPoint;

		public event EventHandler<ErrorEventArgs> OnError;
		public event EventHandler<LogEventArgs> OnLog;

		Dictionary<IPEndPoint, SequentialCommunication> connections;

		internal UDPListener(string host, int port) : base()
		{
			IPAddress bindAddress;
			if (IPAddress.TryParse(host, out bindAddress))
			{
				endPoint = new IPEndPoint(bindAddress, port);
			}
			else
			{
				throw new ArgumentException("Invalid IP address: " + host);
			}

			socket = new UdpClient(endPoint);

			connections = new Dictionary<IPEndPoint, SequentialCommunication>();
		}

		public void Start()
		{
			runningCancellationToken = new CancellationTokenSource();

			Task.Factory.StartNew(() => Run(), runningCancellationToken.Token);
		}

		public void Stop()
		{
			Close();
		}

		async Task Run()
		{
			while (true)
			{
				try
				{
					UdpReceiveResult receive = await Receive();

					IPEndPoint endPoint = receive.RemoteEndPoint;
					byte[] dgram = receive.Buffer;

					SequentialCommunication connection = GetClient(endPoint, dgram);

					byte[] data;
					if (connection != null && AuthenticateDatagram(endPoint, dgram, out data))
					{
						connection.Received(data);

						connection.Flush();
					}
				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, new ErrorEventArgs(ex));
				}
			}
		}

		SequentialCommunication GetClient(IPEndPoint endPoint, byte[] dgram)
		{
			if (connections.ContainsKey(endPoint))
			{
				return connections[endPoint];
			}

			if (dgram.Length != 4)
				return null;

			SequentialCommunication conn = InstantiateConnection(endPoint, dgram);

			if (conn != null)
				connections.Add(endPoint, conn);

			return null;
		}

		internal virtual SequentialCommunication InstantiateConnection(IPEndPoint endPoint, byte[] dgram)
		{
			SendData sendMethod = (data) => Send(endPoint, data);
			DataCallback dataMethod = (data) => OnReceivedData(endPoint, data);

			return new SequentialCommunication(sendMethod, dataMethod);
		}

		protected virtual bool AuthenticateDatagram(IPEndPoint endPoint, byte[] dgram, out byte[] data)
		{
			data = dgram;
			return true;
		}

		protected abstract void OnReceivedData(IPEndPoint endPoint, byte[] data);

		void Send(IPEndPoint endPoint, byte[] dgram)
		{
			SendMessage(dgram, endPoint);
		}
	}
}
