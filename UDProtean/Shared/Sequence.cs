using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean.Shared
{
    internal struct Sequence
    {
		int value;

		public int Value
		{
			get { return value; }
		}

		public void Next()
		{
			value = (value + 1) % SequentialCommunication.SEQUENCE_SIZE;
		}
    }
}
