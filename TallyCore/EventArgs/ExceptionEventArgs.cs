using System;

namespace NetTally.CustomEventArgs
{
    /// <summary>
    /// Custom EventArgs class to pass an exception, and mark whether it was handled.
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
