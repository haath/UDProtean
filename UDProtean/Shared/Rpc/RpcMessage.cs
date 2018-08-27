using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UDProtean.Attributes;

namespace UDProtean.Shared
{
    internal class RpcMessage
    {
		public readonly string ProcName;

		List<string> parameters;

		public int ParameterCount => parameters.Count;

		public RpcMessage(string procName)
		{
			ProcName = procName;
			parameters = new List<string>();
		}

		public RpcMessage AddParameter(object obj)
		{
			string json = JsonConvert.SerializeObject(obj);
			parameters.Add(json);
			return this;
		}

		public object GetParameter(int index)
		{
			return JsonConvert.DeserializeObject(parameters[index]);
		}

		public T GetParameter<T>(int index) where T : new()
		{
			return JsonConvert.DeserializeObject<T>(parameters[index]);
		}

		public byte[] Serialize()
		{
			JObject json = JObject.FromObject(new
			{
				p = ProcName,
				x = JArray.FromObject(parameters)
			});
			return Encoding.ASCII.GetBytes(json.ToString());
		}

		public static RpcMessage Parse(byte[] message)
		{
			JObject json = JObject.Parse(Encoding.ASCII.GetString(message));

			RpcMessage msg = new RpcMessage(json.Value<string>("p"));

			foreach (JToken tkn in json.Value<JArray>("x"))
			{
				msg.parameters.Add(tkn.ToString());
			}

			return msg;
		}

		public static bool TryParse(byte[] message, out RpcMessage msg)
		{
			try
			{
				msg = Parse(message);
				return true;
			}
			catch (JsonException)
			{
				msg = null;
				return false;
			}
		}
	}
}
