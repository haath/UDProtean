using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace UDProtean.Shared
{
	internal class RpcMethodInfo
	{
		public readonly string Name;

		public readonly MethodInfo Method;

		public readonly List<MethodInfo> Deserializers;

		public RpcMethodInfo(string name, MethodInfo method)
		{
			Name = name;
			Method = method;
			Deserializers = new List<MethodInfo>();
		}
	}
}
