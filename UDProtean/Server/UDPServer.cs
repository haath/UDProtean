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
    public class UDPServer
    {
		UdpClient serverSocket;

		CancellationTokenSource runningCancellationToken;

		public event EventHandler<ErrorEventArgs> OnError;

		public event EventHandler<LogEventArgs> OnLog;

		public UDPServer(string host, int port)
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
		}

		public UDPServer(int port) : this("0.0.0.0", port) { }

		public void Start()
		{
			runningCancellationToken = new CancellationTokenSource();
			Task.Run(Run, runningCancellationToken.Token);
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


				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, new ErrorEventArgs(ex));
				}
			}
		}
    }
}
