using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.ViewModels
{
    /// <summary>
    /// EventArgs custom class to pass a message string.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public bool Handled { get; set; }

        public ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
            Handled = false;
        }
    }
}
