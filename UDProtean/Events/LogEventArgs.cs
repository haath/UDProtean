using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDProtean.Events
{
    public class LogEventArgs : EventArgs
    {
        public string Message { get; private set; }

        internal LogEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
