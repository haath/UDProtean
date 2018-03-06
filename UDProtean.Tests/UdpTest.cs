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
		[Test]
		public void TestMethod1()
		{
			UDPServer server = new UDPServer(5000);

			uint expected = 0;

			server.OnData += (ep, data) =>
			{
				uint num = BitConverter.ToUInt32(data, 0);

				Assert.AreEqual(expected++, num);
			};

			Debug.WriteLine(5);

			server.Start();

			Debug.WriteLine(123);

			UDPClient client = new UDPClient("127.0.0.1", 5000);

			client.Connect();

			for (uint i = 0; i < 10000; i++)
			{
				byte[] data = BitConverter.GetBytes(i);
				client.Send(data);
			}

			Thread.Sleep(2000);

			server.Stop();

			Assert.AreEqual(10000, expected);
		}
	}
}
