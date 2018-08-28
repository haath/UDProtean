using ChanceNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		protected int DatagramLength(int min = 500, int max = 5000) => chance.Integer(min, max);

		protected byte[][] TestBuffer(int size = (int)SequentialCommunication.SEQUENCE_SIZE * 3, int datagramMin = 500, int datagramMax = 5000)
		{
			return chance.N<byte[]>(size, () => chance.Hash(DatagramLength(datagramMin, datagramMax))).ToArray();
		}

		protected void Debug(byte[] data)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[")
			  .Append(string.Join(" ", data))
			  .Append("]");
			Console.WriteLine(sb.ToString());
		}
	}
}
