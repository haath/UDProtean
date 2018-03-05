using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDProtean.Events
{
    public class DataEventArgs : EventArgs
    {
        public byte[] RawData { get; private set; }

        internal DataEventArgs(byte[] data)
        {
            RawData = data;
        }
    }
}
