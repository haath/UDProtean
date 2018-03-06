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

		public uint Previous
		{
			get
			{
				return Value > 0 ? Value - 1 : MaxValue;
			}
		}

		public uint Next
		{
			get
			{
				return (value + 1) % SequentialCommunication.SEQUENCE_SIZE;
			}
		}

		public Sequence(uint value)
		{
			this.value = value;
		}

		public uint MaxValue
		{
			get { return SequentialCommunication.SEQUENCE_SIZE - 1; }
		}

		public uint MoveNext()
		{
			value = Next;
			return value;
		}

		public uint MovePrevious()
		{
			value = Previous;
			return value;
		}

		public void Set(uint value)
		{
			this.value = Math.Max(value, 0);
			this.value = Math.Min(this.value, MaxValue);
		}

		public Sequence Clone()
		{
			return new Sequence(value);
		}

		public static bool operator ==(Sequence s1, Sequence s2)
		{
			return s1.value == s2.value;
		}

		public static bool operator !=(Sequence s1, Sequence s2)
		{
			return s1.value != s2.value;
		}
	}
}
