using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDProtean.Events
{
    public class MessageEventArgs : EventArgs
    {
        public byte[] Data { get; private set; }

        internal MessageEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
