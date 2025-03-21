namespace NetTally.Utility.Events;

/// <summary>
/// Custom EventArgs class to pass a message string.
/// </summary>
public class MessageEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
