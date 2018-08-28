using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
	internal struct Datagram
	{
		static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		byte[] data;
		long timestamp;

		public long Age => Timestamp() - timestamp;

		public bool IsEmpty => data == null;

		public int Length => data?.Length ?? 0;

		public byte this[int index] => data[index];

		public Datagram(byte[] data)
		{
			this.data = data;
			timestamp = Timestamp();
		}

		public void Refresh()
		{
			timestamp = Timestamp();
		}

		public static implicit operator Datagram(byte[] data)
		{
			return new Datagram(data);
		}

		public static implicit operator byte[] (Datagram datagram)
		{
			return datagram.data;
		}

		static long Timestamp()
		{
			return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
		}
	}
}
