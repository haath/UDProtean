using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UDProtean.Attributes;

namespace UDProtean.Tests
{
	public class TestHandler
	{
		public int Value = 0;

		public string Label;

		[RpcMethod("set")]
		public void Set(int value)
		{
			Value = value;
		}

		[RpcMethod("add")]
		public void Add(int value)
		{
			Value += value;
		}

		[RpcMethod("set_mult")]
		public void SetMult(int v1, int v2)
		{
			Value = v1 * v2;
		}

		[RpcMethod("repeat")]
		public void Repeat(string str, int times)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < times; i++)
			{
				sb.Append(str);
			}
			Label = sb.ToString();
		}
	}
}
