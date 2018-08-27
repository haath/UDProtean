using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using UDProtean.Attributes;

namespace UDProtean.Shared
{
    internal static class RpcManager
    {
		public static bool IsRpcMessage(byte[] message)
		{
			RpcMessage tmp;
			return RpcMessage.TryParse(message, out tmp);
		}

		public static bool HandleRpcMessage(object obj, byte[] message)
		{
			RpcMessage msg;

			if (!RpcMessage.TryParse(message, out msg))
			{
				return false;
			}

			foreach (MethodInfo method in obj.GetType().GetMethods<RpcMethodAttribute>())
			{
				RpcMethodAttribute rpcAttr = method.GetCustomAttribute<RpcMethodAttribute>();

				if (rpcAttr.Name == msg.ProcName
					&& method.GetParameters().Length == msg.ParameterCount)
				{
					List<object> parameters = new List<object>();

					for (int i = 0; i < msg.ParameterCount; i++)
					{
						ParameterInfo paramInfo = method.GetParameters()[i];

						object param;

						if (paramInfo.ParameterType == typeof(string))
						{
							param = msg.GetParameter(i).ToString();
						}
						else
						{
							MethodInfo getParam = typeof(RpcMessage).GetGenericMethod("GetParameter", paramInfo.ParameterType);

							param = getParam.Invoke(msg, new object[] { i });
						}

						parameters.Add(param);
					}

					method.Invoke(obj, parameters.ToArray());

					return true;
				}
			}

			return false;
		}
    }
}
