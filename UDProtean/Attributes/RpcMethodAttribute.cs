using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean.Attributes
{
	[AttributeUsage(AttributeTargets.Method)]
    public class RpcMethodAttribute : Attribute
    {
		public readonly string Name;

		public RpcMethodAttribute(string name)
		{
			Name = name;
		}
    }
}
