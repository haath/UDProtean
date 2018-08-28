using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("UDProtean.Tests")]

namespace UDProtean
{
	internal delegate void SendData(byte[] data);
	internal delegate void DataCallback(byte[] data);

    internal class SequentialCommunication
    {
		internal const long STALE_AGE_MS = 200;
		internal const uint SEQUENCE_SIZE = 512;
		internal const uint FRAGMENT_SIZE = 540;
		static int SeqIdPool = 0;

		int seqId = SeqIdPool++;

		public static int SequenceBytes
		{
			get
			{
				int bytes = 0;
				while (Math.Pow(2, bytes * 8) < SEQUENCE_SIZE)
					bytes++;
				return bytes;
			}
		}

		Sequence sending;
		Sequence sendingAck;
		Sequence lastAckSent;

		Sequence receiving;
		Sequence processing;

		Datagram[] sendingBuffer;
		Datagram[] receivingBuffer;

		SendData sendData;
		DataCallback callback;

		public SequentialCommunication(SendData sendData, DataCallback callback)
		{
			this.sendData = sendData;
			this.callback = callback;

			sendingBuffer = new Datagram[SEQUENCE_SIZE];
			receivingBuffer = new Datagram[SEQUENCE_SIZE];
		}

		public void Send(byte[] data)
		{
			Debug.Write(seqId, "Sending {0} bytes", data.Length);

			if (data.Length > FRAGMENT_SIZE)
			{
				// Slice datagram into fragments
				byte fragmentCount = 1;
				int start = 0;

				while (start < data.Length)
				{
					int fragmentSize = Math.Min(data.Length - start, (int)FRAGMENT_SIZE);

					byte[] fragment = data.Slice(start, fragmentSize);

					fragment = new byte[] { fragmentCount++ }.Append(fragment);

					start += fragmentSize;

					SendFragment(fragment);
				}
			}
			else
			{
				SendFragment(new byte[] { 0 }.Append(data));
			}
		}

		void SendFragment(byte[] data)
		{
			byte[] sequence = BitConverter.GetBytes(sending.Value)
										  .ToLength(SequenceBytes);

			byte[] dgram = sequence.Append(data);

			Debug.Write(seqId, "Sending sequence: " + sending.Value);

			/*
			 * First, store the datagram in the buffer.
			 */
			sendingBuffer[sending.Value] = dgram;

			/*
			 * Empty the next spot in the circle.
			 * This is done to ensure separation between this
			 * and the previous cycle of values.
			 * Helps clearing the buffer backwards upon
			 * acknowledgements without deleting newer payloads.
			 */
			sendingBuffer[sending.Next] = null;

			SendDatagram(sending);

			sending.MoveNext();
		}

		public void Received(byte[] dgram)
		{
			Debug.Write(seqId, "Received {0} bytes", dgram.Length);

			byte[] sequenceBytes = dgram.Slice(0, SequenceBytes).ToLength(4);
			Sequence sequence = BitConverter.ToUInt32(sequenceBytes, 0);

			/*
			 * If the datagram is only SequenceBytes long, then it's an ACK
			 */
			if (dgram.Length == SequenceBytes)
			{
				ProcessAck(sequence);
				return;
			}

			Debug.Write(seqId, "Received sequence: {0} [lastAck: {1}, recv: {2}]", sequence, lastAckSent, receiving);

			if (sequence.Between(lastAckSent, receiving))
				return;

			byte[] data = dgram.Slice(SequenceBytes);
			receivingBuffer[sequence] = data;

			if (receiving == sequence)
			{
				/*
				 * This datagram is on-par with the sequence.
				 * Handle it.
				 */
				receiving.MoveNext();

				// Advance the receiving sequence on to the receiving buffer.
				ProcessReceivingBuffer();

				if (processing > receiving)
					receiving = processing;

				// Send ack.
				SendAck(receiving.Previous);
			}
			else if (!receivingBuffer[receiving].IsEmpty
				&& receivingBuffer[receiving].Age > STALE_AGE_MS)
			{
				/*
				 * We received a datagram with an unexpected sequence number.
				 * Let the other end know which was the last datagram we received.
				 */
				SendAck(receiving.Previous);
			}
		}
		
		void ProcessReceivingBuffer()
		{
			Debug.Write(seqId, "Starting buffer processing at {0}", processing.Value);

			while (!receivingBuffer[processing.Value].IsEmpty)
			{
				Debug.Write(seqId, "Checking buffer at: " + processing.Value);

				byte[] data = receivingBuffer[processing.Value];
				byte fragment = data[0];
				data = data.Slice(1);

				Debug.Write(seqId, "Fragment num: " + fragment);

				if (fragment == 0)
				{
					receivingBuffer[processing.Value] = null;

					OnData(data);

					processing.MoveNext();
				}
				else if (fragment == 1 && CompleteDatagramAt(processing))
				{
					MemoryStream buffer = new MemoryStream();

					while (!receivingBuffer[processing.Value].IsEmpty)
					{
						byte[] fragData = receivingBuffer[processing.Value];
						byte fragNum = fragData[0];
						fragData = fragData.Slice(1);

						if (fragNum < fragment)
						{
							break;
						}

						Debug.Write(seqId, "Writing fragment {0}.{1}", processing.Value, fragNum);

						receivingBuffer[processing.Value] = null;
						buffer.Write(fragData, 0, fragData.Length);

						fragment++;
						processing.MoveNext();

						if (fragData.Length < FRAGMENT_SIZE)
							break;
					}

					if (buffer.Length > 0)
					{
						buffer.Position = 0;
						byte[] bufferData = new byte[buffer.Length];
						buffer.Read(bufferData, 0, (int)buffer.Length);

						OnData(bufferData);
					}
				}
				else if (fragment != 1)
				{
					Debug.Write(seqId, "ERROR: looking at fragment number {0}", fragment);
					return;
				}
				else
				{
					return;
				}
			}
		}

		bool CompleteDatagramAt(Sequence position)
		{
			byte fragment = 1;

			while (!receivingBuffer[position.Value].IsEmpty)
			{
				byte fragNum = receivingBuffer[position.Value][0];

				Debug.Write(seqId, "Checking fragment {0}", fragNum);

				if (fragNum < fragment
					|| receivingBuffer[position.Value].Length < FRAGMENT_SIZE)
				{
					return true;
				}
				else if (fragNum != fragment)
				{
					Debug.Write(seqId, "ERROR: incosistent fragment {0}->{1}", fragment, fragNum);
				}

				fragment++;
				position.MoveNext();
			}

			return false;
		}

		public void Flush()
		{
			Sequence flush = sendingAck.Next;
			
			while (!sendingBuffer[flush].IsEmpty
				&& flush != sending)
			{
				if (sendingBuffer[flush].Age > STALE_AGE_MS)
				{
					Debug.Write(seqId, "Flushing {0}", flush);

					SendDatagram(flush);

					flush.MoveNext();
				}
			}
		}

		void ProcessAck(uint sequenceNum)
		{
			Debug.Write(seqId, "Received ack: " + sequenceNum);

			if (sendingAck.Value == sequenceNum)
			{
				Sequence bufferClear = sendingAck.Clone();
				sendingAck.MoveNext();

				while (!sendingBuffer[bufferClear.Value].IsEmpty 
					&& bufferClear.Value != sendingAck.Value)
				{
					Debug.Write(seqId, "Clearing sending buffer: " + bufferClear.Value);

					sendingBuffer[bufferClear.Value] = null;
					bufferClear.MovePrevious();
				}

			}
			else
			{
				/*
				 * We received an ACK for a datagram that was not the last on the sequence
				 * Resend the datagrams that are after the one being acknowledged
				 */
				uint toRepeat = (sequenceNum + 1) % SEQUENCE_SIZE;

				sendingAck.Set(toRepeat);

				Debug.Write(seqId, "Repeating sequence: {0}", toRepeat);

				if (!sendingBuffer[toRepeat].IsEmpty)
				{
					Debug.Write(seqId, "Sending sequence: {0}", toRepeat);

					SendDatagram(toRepeat);
				}
			}
		}

		void SendAck(uint sequenceNum)
		{
			Debug.Write(seqId, "Sending ack: " + sequenceNum);

			lastAckSent = sequenceNum;

			receivingBuffer[sequenceNum].Refresh();

			byte[] ack = BitConverter.GetBytes(sequenceNum).ToLength(SequenceBytes);

			sendData?.Invoke(ack);
		}

		protected virtual void SendDatagram(Sequence sequenceNum)
		{
			sendingBuffer[sequenceNum].Refresh();

			sendData?.Invoke(sendingBuffer[sequenceNum]);
		}

		protected virtual void OnData(byte[] data)
		{
			callback?.Invoke(data);
		}
    }
}
