using System;

using UDProtean;
using UDProtean.Shared;
using ChanceNET;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit;
using NUnit.Framework;

namespace UDProtean.Tests
{
	[TestFixture]
	public class SequentialTest : TestBase
	{

		[Test]
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
			};

			comm = new SequentialCommunication(send, null);
			
			foreach (byte[] dgram in buffer)
			{
				comm.Send(dgram);
				expected++;
			}
		}

		[Test]
		public void Receiving()
		{
			SequentialCommunication comm;

			uint next = 0;

			DataCallback callback = (data) =>
			{
				uint received = BitConverter.ToUInt32(data, 0);

				Assert.AreEqual(next, received);
				next++;
			};

			comm = new SequentialCommunication(null, callback);

			Func<uint, byte[], byte[]> genDgram = (seq, data) =>
			{
				byte[] sequence = BitConverter.GetBytes(seq)
										  .ToLength(SequentialCommunication.SequenceBytes);

				return sequence.Append(0).Append(data);
			};

			Action<uint, uint> send = (seq, data) =>
			{
				byte[] dgram = genDgram(seq, BitConverter.GetBytes(data).ToLength(4));
				comm.Received(dgram);
			};

			send(0, 0);
			send(1, 1);
			send(2, 2);
			send(5, 5);
			send(3, 3);
			send(1, 1);
			send(4, 4);
		}

		[TestCase(0.0)]
		[TestCase(0.2)]
		[TestCase(0.3)]
		public void Communicating(double packetLoss)
		{
			Queue<uint> vals = new Queue<uint>(chance.N(ushort.MaxValue * 2, () => (uint)chance.Natural()));
			Queue<uint> toSend = new Queue<uint>(vals);

			SequentialCommunication comm1 = null;			
			SequentialCommunication comm2 = null;

			Action<SequentialCommunication, byte[]> trySend = (comm, data) =>
			{
				if (chance.Bool(1 - packetLoss))
				{
					comm.Received(data);
				}
			};

			Action<uint, byte[]> verify = (expected, data) =>
			{
				uint recv = BitConverter.ToUInt32(data.ToLength(4), 0);
				Assert.AreEqual(expected, recv);
			};

			SendData send1 = (data) =>
			{
				trySend(comm2, data);
			};

			SendData send2 = (data) =>
			{
				trySend(comm1, data);
			};

			DataCallback callback2 = (data) =>
			{
				verify(vals.Dequeue(), data);
			};

			comm1 = new SequentialCommunication(send1, null);
			comm2 = new SequentialCommunication(send2, callback2);

			for (int i = 0; i < vals.Count; i++)
			{
				byte[] data = BitConverter.GetBytes(toSend.Dequeue());
				comm1.Send(data);
			}
		}

		[TestCase(0.0)]
		public void Fragmentation(double packetLoss)
		{
			byte[][] buffer = TestBuffer(datagramMin: 1000, datagramMax: 5000);

			int exp = 0;
			SequentialCommunication comm1 = null;
			SequentialCommunication comm2 = null;

			Action<SequentialCommunication, byte[]> trySend = (comm, data) =>
			{
				if (chance.Bool(1 - packetLoss))
				{
					comm.Received(data);
				}
			};

			DataCallback callback = (data) =>
			{
				CollectionAssert.AreEqual(buffer[exp++], data);
			};

			SendData send1 = (data) =>
			{
				trySend(comm2, data);
			};

			SendData send2 = (data) =>
			{
				trySend(comm1, data);
			};

			comm1 = new SequentialCommunication(send1, null);
			comm2 = new SequentialCommunication(send2, callback);

			for (int i = 0; i < buffer.Length; i++)
			{
				byte[] data = buffer[i];
				comm1.Send(data);
			}
		}
	}
}
