using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using UDProtean.Events;
using UDProtean.Shared;

namespace UDProtean.Server
{
    public abstract class UDPListener
    {
		UdpClient serverSocket;

		CancellationTokenSource runningCancellationToken;

		public event EventHandler<ErrorEventArgs> OnError;

		public event EventHandler<LogEventArgs> OnLog;

		Dictionary<IPEndPoint, SequentialCommunication> connections;

		internal UDPListener(string host, int port)
		{
			IPAddress bindAddress;
			if (IPAddress.TryParse(host, out bindAddress))
			{
				IPEndPoint endPoint = new IPEndPoint(bindAddress, port);

				serverSocket = new UdpClient(endPoint);
			}
			else
			{
				throw new ArgumentException("Invalid IP addres: " + host);
			}

			connections = new Dictionary<IPEndPoint, SequentialCommunication>();
		}

		public void Start()
		{
			runningCancellationToken = new CancellationTokenSource();

			Task.Factory.StartNew(() => Run(), runningCancellationToken.Token);
		}

		public void Stop()
		{
			runningCancellationToken.Cancel();
		}

		async Task Run()
		{
			while (true)
			{
				try
				{
					UdpReceiveResult receive = await serverSocket.ReceiveAsync();

					IPEndPoint endPoint = receive.RemoteEndPoint;
					byte[] dgram = receive.Buffer;

					SequentialCommunication connection = GetClient(endPoint, dgram);

					byte[] data;
					if (connection != null && AuthenticateDatagram(endPoint, dgram, out data))
					{
						connection.Received(data);
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

			SequentialCommunication conn = InstantiateConnection(endPoint, dgram);

			if (conn != null)
				connections.Add(endPoint, conn);

			return conn;
		}

		internal virtual SequentialCommunication InstantiateConnection(IPEndPoint endPoint, byte[] dgram)
		{
			SendData sendMethod = (data) => Send(endPoint, data).Wait();
			DataCallback dataMethod = (data) => OnReceivedData(endPoint, data);

			return new SequentialCommunication(sendMethod, dataMethod);
		}

		protected virtual bool AuthenticateDatagram(IPEndPoint endPoint, byte[] dgram, out byte[] data)
		{
			data = dgram;
			return true;
		}

		protected abstract void OnReceivedData(IPEndPoint endPoint, byte[] data);

		async Task Send(IPEndPoint endPoint, byte[] dgram)
		{
			await serverSocket.SendAsync(dgram, dgram.Length, endPoint);
		}
	}
}
