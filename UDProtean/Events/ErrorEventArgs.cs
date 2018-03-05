using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDProtean.Events
{
    public class ErrorEventArgs : EventArgs
    {
		public Exception Exception { get; private set; }

		internal ErrorEventArgs(Exception exception)
        {
			Exception = exception;
        }
    }
}
