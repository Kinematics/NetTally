using System;

namespace NetTally.CustomEventArgs
{
    /// <summary>
    /// Custom EventArgs class to pass a message string.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
