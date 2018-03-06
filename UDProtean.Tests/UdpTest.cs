using System;

using UDProtean;
using UDProtean.Client;
using UDProtean.Server;
using UDProtean.Shared;
using ChanceNET;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using NUnit;
using NUnit.Framework;

namespace UDProtean.Tests
{
	[TestFixture]
	public class UdpTest : TestBase
	{
		[TestCase(0.0)]
		[TestCase(0.3)]
		public void ServerReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer server = new UDPServer(5000);

			uint expected = 0;

			server.OnData += (ep, data) =>
			{
				uint num = BitConverter.ToUInt32(data, 0);

				Assert.AreEqual(expected++, num);
			};

			server.Start();

			UDPClient client = new UDPClient("127.0.0.1", 5000);

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
			}

			Thread.Sleep(1000);

			server.Stop();
		}

		[TestCase(0.0)]
		[TestCase(0.3)]
		public void ServerBehaviorReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer<TestBehavior> server = new UDPServer<TestBehavior>(5000);
			server.Start();

			UDPClient client = new UDPClient("127.0.0.1", 5000);

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
			}

			Thread.Sleep(1000);

			server.Stop();
		}

		[TestCase(0.0)]
		[TestCase(0.3)]
		public void BidirectionalReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer<TestBehavior> server = new UDPServer<TestBehavior>(5000);
			server.Start();

			UDPClient client = new UDPClient("127.0.0.1", 5000);

			uint expected = 0;
			client.OnData += (s, d) =>
			{
				uint num = BitConverter.ToUInt32(d.Data, 0);
				Assert.AreEqual(expected++, num);
			};

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
			}

			Thread.Sleep(1000);

			server.Stop();
		}
	}
}
