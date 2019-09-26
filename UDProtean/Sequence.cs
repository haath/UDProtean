using System;
using System.Collections.Generic;
using System.Text;

namespace UDProtean
{
    internal struct Sequence
    {
		uint value;

		public uint Value => value;

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

		public bool Between(Sequence s1, Sequence s2)
		{
			return (s1 < s2 && s1 < this && this < s2)      // [ .. s1 .. this .. s2 .. ]
				|| (s1 > s2 && (s1 < this || this < s2));   // [ .. this .. s2 .. s1 .. ]
															// [ .. s2 .. s1 .. this .. ]
		}

		public override string ToString()
		{
			return value.ToString();
		}

		public uint DistanceTo(Sequence seq)
		{
			if (seq >= this)
			{
				return seq.value - this.value;
			}
			else
			{
				return SequentialCommunication.SEQUENCE_SIZE - this.value + seq.value;
			}
		}

        public bool IsBefore(Sequence seq)
        {
            return !IsAfter(seq) && this != seq;
        }

        public bool IsAfter(Sequence seq)
        {
            return DistanceTo(seq) >= 16;
        }

		public static bool operator ==(Sequence s1, Sequence s2)
		{
			return s1.value == s2.value;
		}

		public static bool operator !=(Sequence s1, Sequence s2)
		{
			return s1.value != s2.value;
		}

		public static bool operator <(Sequence s1, Sequence s2)
		{
			return s1.value < s2.value;
		}

		public static bool operator >(Sequence s1, Sequence s2)
		{
			return s1.value > s2.value;
		}

		public static implicit operator Sequence(uint value)
		{
			return new Sequence(value);
		}

		public static implicit operator uint(Sequence sequence)
		{
			return sequence.value;
		}
	}
}
