using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UDProtean.Shared;

namespace UDProtean.Server
{
    public abstract class UDPClientBehavior
    {
		protected IPEndPoint EndPoint { get; private set; }

		SendData sendMethod;

		internal void _OnOpen(IPEndPoint endPoint, SendData sendMethod)
		{
			EndPoint = endPoint;
		}

		internal void _OnData(byte[] data)
		{
			OnData(data);
		}

		protected abstract void OnOpen();

		protected abstract void OnClose();

		protected abstract void OnData(byte[] data);

		protected abstract void OnError(Exception ex);

		public void Send(byte[] data)
		{
			sendMethod?.Invoke(data);
		}
    }
}
