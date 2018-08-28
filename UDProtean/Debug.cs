using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
	internal static class Debug
	{
		static StackFrame CallingFrame
		{
			get
			{
				StackFrame frame;
				int skip = 2;

				do
				{
					frame = new StackFrame(skip++, true);
				}
				while (frame.GetMethod().DeclaringType == typeof(Debug));

				return frame;

			}
		}

#if DEBUG
		public static void Write(int seqId, string line)
		{
			StackFrame curFrame = CallingFrame;
			string type = curFrame.GetMethod().DeclaringType.Name;
			string method = curFrame.GetMethod().Name;

			Console.WriteLine("{0}.{1} => {2}", seqId, method, line);
		}
#else
		public static void Write(int seqId, string line) { }
#endif

		public static void Write(int seqId, string format, params object[] args)
		{
			Write(seqId, string.Format(format, args));
		}

		public static string Visualize(this byte[] buffer)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[")
			  .Append(string.Join(" ", buffer))
			  .Append("]");
			return sb.ToString();
		}
	}
}
