using System;
using System.Collections.Generic;
using System.Linq;

using NUnit;
using NUnit.Framework;

using UDProtean.Shared;

namespace UDProtean.Tests
{
	[TestFixture]
	public class RpcTests
	{
		static IEnumerable<object[]> SerializationTestCases()
		{
			yield return new object[]
				{
					new RpcMessage("repeat").AddParameter("enlo").AddParameter(1)
				};
			yield return new object[]
				{
					new RpcMessage("enlo").AddParameter(DateTime.Now)
				};
			yield return new object[]
				{
					new RpcMessage("enlo").AddParameter(DateTime.Now).AddParameter(new { asdf = 5 })
				};
		}

		[Test, TestCaseSource("SerializationTestCases")]
		public void SerializationTest(object msgObj)
		{
			RpcMessage msg = (RpcMessage)msgObj;

			RpcMessage other = RpcMessage.Parse(msg.Serialize());

			Assert.AreEqual(msg.ProcName, other.ProcName);
			Assert.AreEqual(msg.ParameterCount, other.ParameterCount);

			for (int i = 0; i < msg.ParameterCount; i++)
			{
				Assert.AreEqual(msg.GetParameter(i), other.GetParameter(i));
			}
		}

		static IEnumerable<object[]> IntegerTestCases()
		{
			yield return new object[]
				{
					new RpcMessage("set").AddParameter(3),
					3
				};
			yield return new object[]
				{
					new RpcMessage("set").AddParameter(-19),
					-19
				};
			yield return new object[]
				{
					new RpcMessage("add").AddParameter(7),
					14
				};
			yield return new object[]
				{
					new RpcMessage("set_mult").AddParameter(-8).AddParameter(7),
					-56
				};
		}

		[Test, TestCaseSource("IntegerTestCases")]
		public void IntegerTest(object msgObj, int expected)
		{
			RpcMessage msg = (RpcMessage)msgObj;

			TestHandler handler = new TestHandler();

			RpcManager.HandleRpcMessage(handler, msg.Serialize());
			RpcManager.HandleRpcMessage(handler, msg.Serialize());

			Assert.AreEqual(expected, handler.Value);
		}

		static IEnumerable<object[]> StringTestCases()
		{
			yield return new object[]
				{
					new RpcMessage("repeat").AddParameter("enlo").AddParameter(1),
					"enlo"
				};
			yield return new object[]
				{
					new RpcMessage("repeat").AddParameter("enlo").AddParameter(4),
					"enloenloenloenlo"
				};
			yield return new object[]
				{
					new RpcMessage("repeat").AddParameter("enlo").AddParameter(-12),
					""
				};
		}

		[Test, TestCaseSource("StringTestCases")]
		public void StringTest(object msgObj, string expected)
		{
			RpcMessage msg = (RpcMessage)msgObj;

			TestHandler handler = new TestHandler();

			RpcManager.HandleRpcMessage(handler, msg.Serialize());

			Assert.AreEqual(expected, handler.Label);
		}
	}
}
