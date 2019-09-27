using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
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

		public static byte[] ToLength(this byte[] buffer, int length)
		{
			if (buffer.Length < length)
			{
				byte[] padding = new byte[length - buffer.Length];


				if (BitConverter.IsLittleEndian)
				{
					return buffer.Append(padding);
				}
				else
				{
					return padding.Append(buffer);
				}
			}
			else if (buffer.Length > length)
			{
				if (BitConverter.IsLittleEndian)
				{
					return buffer.Slice(0, length);
				}
				else
				{
					return buffer.Slice(buffer.Length - length);
				}
			}
			else
			{
				return buffer.Clone() as byte[];
			}
		}

		public static string ToHex(this byte[] buffer)
		{
			string hex = BitConverter.ToString(buffer);
			return hex.Replace("-", "");
		}

		public static byte[] HexToBytes(this string hex)
		{
			byte[] retval = new byte[hex.Length / 2];
			for (int i = 0; i < hex.Length; i += 2)
				retval[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return retval;
		}

		public static Task ConnectAsync(this Socket socket, IPAddress ipAddr, int port)
		{
			return Task.Factory.FromAsync(
				socket.BeginConnect(ipAddr, port, null, socket),
				socket.EndConnect
			);
		}

		public static Task SendAsync(this Socket socket, IPEndPoint dest, byte[] data)
		{
			return Task.Factory.FromAsync(
				socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, dest, null, socket),
				socket.EndSendTo
			);
		}
	}
}
