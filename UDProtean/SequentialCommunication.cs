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
		internal const uint SEQUENCE_SIZE = 512;
		internal const uint FRAGMENT_SIZE = 540;

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

		byte[][] sendingBuffer;
		byte[][] receivingBuffer;

		SendData sendData;
		DataCallback callback;

		public SequentialCommunication(SendData sendData, DataCallback callback)
		{
			this.sendData = sendData;
			this.callback = callback;

			sendingBuffer = new byte[SEQUENCE_SIZE][];
			receivingBuffer = new byte[SEQUENCE_SIZE][];
		}

		public void Send(byte[] data)
		{
			Debug.Write("Sending {0} bytes", data.Length);

			if (data.Length > FRAGMENT_SIZE)
			{
				// Slice datagram into fragments
				byte fragmentCount = 1;
				int start = 0;

				while (start <= data.Length)
				{
					int fragmentSize = Math.Min(data.Length - start, (int)FRAGMENT_SIZE);

					byte[] fragment = data.Slice(start, fragmentSize);

					Debug.Write("Fragment {0}, {1} bytes", fragmentCount, fragmentSize);

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

			Debug.Write("Sending sequence: " + sending.Value);

			/*
			 * First, store the datagram in the buffer.
			 */
			sendingBuffer[sending.Value] = dgram;
			sending.MoveNext();

			/*
			 * Empty the next spot in the circle.
			 * This is done to ensure separation between this
			 * and the previous cycle of values.
			 * Helps clearing the buffer backwards upon
			 * acknowledgements without deleting newer payloads.
			 */
			sendingBuffer[sending.Value] = null;

			SendData(dgram);
		}

		public void Received(byte[] dgram)
		{
			Debug.Write("Received {0} bytes", dgram.Length);

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

			Debug.Write("Received sequence: {0}  [{1}/{2}]", sequence, lastAckSent, receiving);

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

				// Send ack.
				SendAck(receiving.Previous);
			}
			else
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
			Debug.Write("Starting buffer processing at {0}", processing.Value);

			while (receivingBuffer[processing.Value] != null)
			{
				Debug.Write("Checking buffer at: " + processing.Value);

				byte[] data = receivingBuffer[processing.Value];
				byte fragment = data[0];
				data = data.Slice(1);

				Debug.Write("Fragment num: " + fragment);

				if (fragment == 0)
				{
					receivingBuffer[processing.Value] = null;

					OnData(data);

					processing.MoveNext();
				}
				else if (fragment == 1 && CompleteDatagramAt(processing))
				{
					MemoryStream buffer = new MemoryStream();

					while (receivingBuffer[processing.Value] != null)
					{
						byte[] fragData = receivingBuffer[processing.Value];
						byte fragNum = fragData[0];
						fragData = fragData.Slice(1);

						if (fragNum < fragment)
						{
							break;
						}

						Debug.Write("Writing fragment {0}.{1}", processing.Value, fragNum);

						receivingBuffer[processing.Value] = null;
						buffer.Write(fragData, 0, fragData.Length);

						fragment++;
						processing.MoveNext();
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
					Debug.Write("ERROR: looking at fragment number {0}", fragment);
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

			while (receivingBuffer[position.Value] != null)
			{
				byte fragNum = receivingBuffer[position.Value][0];

				Debug.Write("Checking fragment {0}", fragNum);

				if (fragNum < fragment)
				{
					return true;
				}
				else if (fragNum != fragment)
				{
					Debug.Write("ERROR: incosistent fragment {0}->{1}", fragment, fragNum);
				}

				fragment++;
				position.MoveNext();
			}

			return false;
		}

		void ProcessAck(uint sequenceNum)
		{
			Debug.Write("Received ack: " + sequenceNum);

			if (sendingAck.Value == sequenceNum)
			{
				Sequence bufferClear = sendingAck.Clone();
				sendingAck.MoveNext();

				while (sendingBuffer[bufferClear.Value] != null 
					&& bufferClear.Value != sendingAck.Value)
				{
					Debug.Write("Clearing sending buffer: " + bufferClear.Value);

					sendingBuffer[bufferClear.Value] = null;
					bufferClear.MovePrevious();
				}

			}
			else
			{
				/*
				 * We received an ACK for a datagram that was not the last on the sequence
				 * Resend the datagram that is after the one being acknowledged
				 */
				uint toRepeat = (sequenceNum + 1) % SEQUENCE_SIZE;
				byte[] dgramToRepeat = sendingBuffer[toRepeat];

				sendingAck.Set(toRepeat);

				Debug.Write("Repeating sequence: {0}", toRepeat);

				if (dgramToRepeat != null)
				{
					Debug.Write("Sending sequence: {0}", toRepeat);

					SendData(dgramToRepeat);
				}
			}
		}

		void SendAck(uint sequenceNum)
		{
			Debug.Write("Sending ack: " + sequenceNum);

			lastAckSent = sequenceNum;

			byte[] ack = BitConverter.GetBytes(sequenceNum).ToLength(SequenceBytes);
			SendData(ack);
		}

		protected virtual void SendData(byte[] data)
		{
			Debug.Write("Sending {0} bytes", data.Length);

			sendData?.Invoke(data);
		}

		protected virtual void OnData(byte[] data)
		{
			Debug.Write("Handling {0} bytes", data.Length);

			callback?.Invoke(data);
		}
    }
}
