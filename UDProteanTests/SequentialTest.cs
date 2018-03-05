using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using UDProtean;
using UDProtean.Shared;
using ChanceNET;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace UDProteanTests
{
	[TestClass]
	public class SequentialTest
	{
		private TestContext testContextInstance;

		/// <summary>
		///  Gets or sets the test context which provides
		///  information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get { return testContextInstance; }
			set { testContextInstance = value; }
		}

		Chance chance = new Chance();

		int DatagramLength() => chance.Integer(500, 5000);

		byte[][] TestBuffer()
		{
			return chance.N<byte[]>((int)SequentialCommunication.SEQUENCE_SIZE * 3, () => chance.Hash(DatagramLength())).ToArray();
		}


		[TestMethod]
		public void Utils()
		{
			byte[] b1 = chance.Hash(DatagramLength());
			byte[] b2 = chance.Hash(DatagramLength());

			byte[] joined = b1.Append(b2);

			Assert.AreEqual(b1.Length + b2.Length, joined.Length);

			CollectionAssert.AreEqual(b1, joined.Slice(0, b1.Length));
			CollectionAssert.AreEqual(b2, joined.Slice(b1.Length, b2.Length));

			CollectionAssert.AreEqual(b1, b1.ToLength(b1.Length));
		}

		[TestMethod]
		public void Sending()
		{
			byte[][] buffer = TestBuffer();
			uint expected = 0;

			SequentialCommunication comm;

			SendData send = (data) =>
			{
				byte[] expectedBuffer = buffer[expected];

				Assert.AreEqual(expectedBuffer.Length, data.Length - SequentialCommunication.SequenceBytes);

				byte[] sequence = data.Slice(0, SequentialCommunication.SequenceBytes).ToLength(4);
				uint sequenceNum = BitConverter.ToUInt32(sequence, 0);

				Assert.AreEqual(expected % SequentialCommunication.SEQUENCE_SIZE, BitConverter.ToUInt32(sequence, 0));


				data = data.Slice(SequentialCommunication.SequenceBytes);
				CollectionAssert.AreEqual(expectedBuffer, data);

				return Task.CompletedTask;
			};

			comm = new SequentialCommunication(send, null);
			
			foreach (byte[] dgram in buffer)
			{
				comm.Send(dgram).Wait();
				expected++;
			}
		}

		[TestMethod]
		public void Receiving()
		{
			SequentialCommunication comm;

			uint next = 0;

			DataCallback callback = (data) =>
			{
				uint received = BitConverter.ToUInt32(data, 0);

				Assert.AreEqual(next, received);
				next++;

				return Task.CompletedTask;
			};

			comm = new SequentialCommunication(null, callback);

			Func<uint, byte[], byte[]> genDgram = (seq, data) =>
			{
				byte[] sequence = BitConverter.GetBytes(seq)
										  .ToLength(SequentialCommunication.SequenceBytes);

				return sequence.Append(data);
			};

			Action<uint, uint> send = (seq, data) =>
			{
				byte[] dgram = genDgram(seq, BitConverter.GetBytes(data).ToLength(4));
				comm.Received(dgram).Wait();
			};

			send(0, 0);
			send(1, 1);
			send(2, 2);
			send(5, 5);
			send(3, 3);
			send(1, 1);
			send(4, 4);
		}
	}
}
