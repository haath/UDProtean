using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace UDProtean.Server
{
	public class UDPServer : UDPListener
	{
		public event Action<IPEndPoint, byte[]> OnData;

		public UDPServer(string host, int port) : base(host, port)
		{
		}

		public UDPServer(int port) : this("0.0.0.0", port) { }

		protected override void OnReceivedData(IPEndPoint endPoint, byte[] data)
		{
			OnData?.Invoke(endPoint, data);
		}
	}
}
