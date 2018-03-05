using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDProtean.Shared
{
	internal delegate Task SendData(byte[] data);
	internal delegate Task DataCallback(byte[] data);

    internal class SequentialCommunication
    {
		internal const int SEQUENCE_SIZE = 256;

		int SequenceBytes
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
		Sequence receiving;
		Sequence ack;

		byte[][] sendingBuffer;

		SendData sendData;
		DataCallback callback;

		public SequentialCommunication(SendData sendData, DataCallback callback)
		{
			this.sendData = sendData;
			this.callback = callback;

			sendingBuffer = new byte[SEQUENCE_SIZE][];
		}

		public async Task Send(byte[] data)
		{
			byte[] sequence = BitConverter.GetBytes(sending.Value);

			if (sequence.Length < SequenceBytes)
			{
				sequence = sequence.PadLeft(SequenceBytes);
			}

			byte[] dgram = sequence.Append(data);

			/*
			 * First, store the datagram in the buffer
			 */
			sendingBuffer[sending.Value] = dgram;
			sending.Next();

			await sendData(dgram);
		}

		public async Task Received(byte[] dgram)
		{
			byte[] sequence = dgram.Slice(0, SequenceBytes);
			int sequenceNum = BitConverter.ToInt32(sequence, 0);

			/*
			 * If the datagram is only SequenceBytes long, then it's an ACK
			 */
			if (dgram.Length == SequenceBytes)
			{
				await ProcessAck(sequenceNum);
				return;
			}

			byte[] data = dgram.Slice(SequenceBytes);

			if (receiving.Value == sequenceNum)
			{
				/*
				 * This datagram is on-par with the sequence
				 * Handle it
				 */
				
				// Send ack
				await SendAck(receiving.Value);

				// Increment the receiving sequence
				receiving.Next();

				// Invoke the handler
				await callback(data);
			}
			else
			{
				/*
				 * We received a datagram with an unexpected sequence number
				 * Let the other end know which was the last datagram we received
				 */
				await SendAck(receiving.Value);
			}
		}

		async Task ProcessAck(int sequenceNum)
		{
			if (ack.Value == sequenceNum)
			{
				ack.Next();
			}
			else
			{
				/*
				 * We received an ACK for a datagram that was not the last on the sequence
				 * Resend the datagram that is after the one being acknowledged
				 */
				byte[] dgramToRepeat = sendingBuffer[(sequenceNum + 1) % SEQUENCE_SIZE];

				if (dgramToRepeat != null)
				{
					await sendData(dgramToRepeat);
				}
			}
		}

		Task SendAck(int sequenceNum)
		{
			byte[] ack = BitConverter.GetBytes(sequenceNum);
			return sendData(ack);
		}
    }
}
