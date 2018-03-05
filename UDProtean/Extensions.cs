using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
	internal static class Extensions
    {
		public static byte[] Slice(this byte[] buffer, int start, int length)
		{
			byte[] sliced = new byte[length];
			Array.Copy(buffer, start, sliced, 0, length);
			return sliced;
		}

		public static byte[] Slice(this byte[] buffer, int start)
		{
			return buffer.Slice(start, buffer.Length - start);
		}

		public static byte[] Append(this byte[] buffer, byte[] other)
		{
			byte[] joined = new byte[buffer.Length + other.Length];

			Array.Copy(buffer, joined, buffer.Length);
			Array.Copy(other, 0, joined, buffer.Length, other.Length);

			return joined;
		}

		public static byte[] Append(this byte[] buffer, byte other)
		{
			return buffer.Append(new byte[] { other });
		}

		public static byte[] PadLeft(this byte[] buffer, int length)
		{
			if (length <= buffer.Length)
				return buffer.Clone() as byte[];

			byte[] padding = new byte[length - buffer.Length];

			return padding.Append(buffer);
		}
	}
}
