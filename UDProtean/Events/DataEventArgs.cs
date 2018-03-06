using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDProtean.Events
{
    public class DataEventArgs : EventArgs
    {
        public byte[] Data { get; private set; }

        internal DataEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
