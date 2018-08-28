using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace UDProtean.Server
{
    public class UDPServer<ClientBehavior> : UDPListener 
		where ClientBehavior : UDPClientBehavior, new()
	{
		Dictionary<IPEndPoint, ClientBehavior> connections;

		public UDPServer(string host, int port) : base(host, port)
		{
			connections = new Dictionary<IPEndPoint, ClientBehavior>();
		}

		public UDPServer(int port) : this("0.0.0.0", port) { }

		internal override SequentialCommunication InstantiateConnection(IPEndPoint endPoint, byte[] dgram)
		{
			ClientBehavior behavior = new ClientBehavior();

			connections.Add(endPoint, behavior);

			behavior.OnOpen(
				endPoint,
				new SendData((data) => SendMessage(data, endPoint))
			);

			return base.InstantiateConnection(endPoint, dgram);
		}

		protected override bool AuthenticateDatagram(IPEndPoint endPoint, byte[] dgram, out byte[] data)
		{
			if (!connections.ContainsKey(endPoint))
			{
				data = null;
				return false;
			}

			return base.AuthenticateDatagram(endPoint, dgram, out data);
		}

		protected override void OnReceivedData(IPEndPoint endPoint, byte[] data)
		{
			ClientBehavior client = connections[endPoint];
			client.OnData(data);
		}
	}
}
