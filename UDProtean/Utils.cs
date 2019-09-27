using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
    public static class Utils
    {
		public static string GenerateHandshake()
		{
			return "ffff" + new Random().Next().ToString("x8");
		}

		public static bool IsHandshake(byte[] datagram)
		{
			return datagram.Length == 6 
				&& datagram.ToHex().ToLower().StartsWith("ffff");
		}
    }
}
