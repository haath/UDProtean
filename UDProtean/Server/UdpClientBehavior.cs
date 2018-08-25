using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using UDProtean.Attributes;
using UDProtean.Shared;

namespace UDProtean.Server
{
    public abstract class UDPClientBehavior
    {
		protected IPEndPoint EndPoint { get; private set; }

		SendData sendMethod;

		internal void OnOpen(IPEndPoint endPoint, SendData sendMethod)
		{
			EndPoint = endPoint;
			this.sendMethod = sendMethod;
		}

		internal void OnData(byte[] data)
		{
			OnMessage(data);
		}

		protected abstract void OnOpen();

		protected abstract void OnClose();

		protected abstract void OnMessage(byte[] data);

		protected abstract void OnError(Exception ex);

		public void Send(byte[] data)
		{
			sendMethod?.Invoke(data);
		}
    }
}
