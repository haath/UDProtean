using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean.Shared
{
    internal struct Sequence
    {
		uint value;

		public uint Value
		{
			get { return value; }
		}

		public void Next()
		{
			value = (value + 1) % SequentialCommunication.SEQUENCE_SIZE;
		}
    }
}
