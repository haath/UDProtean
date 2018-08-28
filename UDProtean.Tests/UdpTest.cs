using System;

using UDProtean;
using UDProtean.Client;
using UDProtean.Server;
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
		static int port = new Random().Next(1024, 60000);

		Queue<IDisposable> disposables = new Queue<IDisposable>();

		[Test]
		public void Constructors()
		{
			UDPServer server = GetServer();
			UDPClient client = GetClient();
			UDPServer<TestBehavior> serverT = GetServer<TestBehavior>();
		}
		
		[TestCase(0.0)]
		[TestCase(0.1)]
		public void ServerReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer server = GetServer();

			uint expected = 0;

			server.OnData += (ep, data) =>
			{
				uint num = BitConverter.ToUInt32(data, 0);

				Assert.AreEqual(expected++, num);
			};

			server.Start();

			UDPClient client = GetClient();

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
				Thread.Sleep(0);
			}

			Thread.Sleep(1000);
		}
		
		public void ServerBehaviorReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer<TestBehavior> server = GetServer<TestBehavior>();
			server.Start();

			UDPClient client = GetClient();

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
			}

			Thread.Sleep(1000);
		}
		
		public void BidirectionalReceiving(double packetLoss)
		{
			UDPSocket.PACKET_LOSS = packetLoss;

			UDPServer<TestBehavior> server = GetServer<TestBehavior>();
			server.Start();

			UDPClient client = GetClient();

			uint expected = 0;
			client.OnMessage += (s, d) =>
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
		}

		UDPClient GetClient()
		{
			UDPClient client = new UDPClient("127.0.0.1", port - 1);
			disposables.Enqueue(client);
			return client;
		}

		UDPServer GetServer()
		{
			UDPServer server = new UDPServer(port++);
			disposables.Enqueue(server);
			return server;
		}

		UDPServer<T> GetServer<T>() where T : Server.UDPClientBehavior, new()
		{
			UDPServer<T> server = new UDPServer<T>(port++);
			disposables.Enqueue(server);
			return server;
		}

		[TearDown]
		public void Dispose()
		{
			foreach (IDisposable disposable in disposables)
				disposable.Dispose();
		}
	}
}
