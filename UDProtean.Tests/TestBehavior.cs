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
	public class TestBehavior : UDPClientBehavior
	{
		uint expected = 0;

		protected override void OnClose()
		{
		}

		protected override void OnData(byte[] data)
		{
			uint num = BitConverter.ToUInt32(data, 0);
			Assert.AreEqual(expected++, num);

			Send(data);
		}

		protected override void OnError(Exception ex)
		{
		}

		protected override void OnOpen()
		{
		}
	}
}
