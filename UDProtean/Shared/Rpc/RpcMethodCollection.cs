using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using UDProtean.Attributes;

namespace UDProtean.Shared
{
	internal class RpcMethodCollection
	{
		Dictionary<string, RpcMethodInfo> methods = new Dictionary<string, RpcMethodInfo>();

		public void AddType<T>()
		{
			foreach (MethodInfo method in typeof(T).GetMethods<RpcMethodAttribute>())
			{
				RpcMethodAttribute rpcAttr = method.GetCustomAttribute<RpcMethodAttribute>();

				RpcMethodInfo methodInfo = new RpcMethodInfo(rpcAttr.Name, method);

				for (int i = 0; i < method.GetParameters().Length; i++)
				{
					ParameterInfo paramInfo = method.GetParameters()[i];

					MethodInfo deserializer;

					if (paramInfo.ParameterType == typeof(string))
					{
						deserializer = typeof(RpcMessage).GetMethod("GetParameter");
					}
					else
					{
						deserializer = typeof(RpcMessage).GetGenericMethod("GetParameter", paramInfo.ParameterType);
					}

					parameters.Add(param);
				}
			}
		}
	}
}
