using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UDProtean.Shared;

namespace UDProtean.Server
{
    public abstract class UDPClientBehavior
    {

		protected IPEndPoint endPoint { get; private set; }

		internal void _OnOpen(IPEndPoint endPoint)
		{
			this.endPoint = endPoint;
		}

		internal void _OnData(byte[] data)
		{
			OnData(data);
		}

		protected abstract void OnOpen();

		protected abstract void OnClose();

		protected abstract void OnData(byte[] data);

		protected abstract void OnError(Exception ex);
    }
}
