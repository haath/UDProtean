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
		internal const int SEQUENCE_SIZE = 512;
		internal const int FRAGMENT_SIZE = 540;
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

            int fragmentCount = (data.Length / FRAGMENT_SIZE) + 1;
            int dataIndex = 0;

            while (fragmentCount > 0)
            {
                fragmentCount--;

                int fragmentSize = Math.Min(data.Length - dataIndex, FRAGMENT_SIZE);

                byte[] fragment = data.Slice(dataIndex, fragmentSize);
                fragment = new byte[] { (byte)fragmentCount }.Append(fragment);
                dataIndex += fragmentSize;

                SendFragment(fragment);
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

                int fragmentCount = CompleteDatagramAt(processing);

                if (fragmentCount > 0)
                {
                    MemoryStream bufferStream = new MemoryStream();

                    while (fragmentCount > 0)
                    {
                        byte[] fragData = receivingBuffer[processing.Value];
                        byte fragNum = fragData[0];
                        fragData = fragData.Slice(1);

                        Debug.Write(seqId, "Writing fragment {0}.{1}", processing.Value, fragNum);
                        bufferStream.Write(fragData, 0, fragData.Length);

                        receivingBuffer[processing.Value] = null;
                        fragmentCount--;
                        processing.MoveNext();
                    }

                    byte[] buffer = new byte[bufferStream.Length];

                    bufferStream.Position = 0;
                    bufferStream.Read(buffer, 0, (int)bufferStream.Length);
                    bufferStream.Dispose();

                    OnData(buffer);
                }
                else
                {
                    return;
                }
			}
		}

		int CompleteDatagramAt(Sequence position)
		{
            int fragmentCount = 0;
            int previousFragmentNum = 0;

			while (!receivingBuffer[position.Value].IsEmpty)
			{
				byte fragNum = receivingBuffer[position.Value][0];

                fragmentCount++;

                Debug.Write(seqId, "Checking fragment {0}", fragNum);

				if (fragNum == 0)
				{
					return fragmentCount;
				}

                if (previousFragmentNum > 0
                    && fragNum != previousFragmentNum - 1)
				{
					Debug.Write(seqId, "ERROR: incosistent fragment {0}->{1}", previousFragmentNum, fragNum);
				}

                previousFragmentNum = fragNum;
                position.MoveNext();
			}

			return 0;
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
				}
				flush.MoveNext();
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
