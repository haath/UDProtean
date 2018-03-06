using ChanceNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDProtean.Shared;

namespace UDProtean.Tests
{
	public class TestBase
	{
		Chance _chance;

		protected Chance chance
		{
			get
			{
				if (_chance == null)
				{
					_chance = new Chance(613509781);
					Console.WriteLine("Seed: " + _chance.GetSeed());
				}
				return _chance;
			}
		}

		protected int DatagramLength() => chance.Integer(500, 5000);

		protected byte[][] TestBuffer()
		{
			return chance.N<byte[]>((int)SequentialCommunication.SEQUENCE_SIZE * 3, () => chance.Hash(DatagramLength())).ToArray();
		}
	}
}
