namespace NetTally.CustomEventArgs
{
    /// <summary>
    /// Custom EventArgs class to pass an exception, and mark whether it was handled.
    /// </summary>
    public class ExceptionEventArgs(Exception exception) : EventArgs
    {
        public Exception Exception { get; } = exception;
        public bool Handled { get; set; } = false;
    }
}
