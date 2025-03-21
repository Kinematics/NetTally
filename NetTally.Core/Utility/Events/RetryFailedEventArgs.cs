namespace NetTally.Utility.Events;

public class RetryFailedEventArgs(
    string url,
    int retryCount,
    Exception exception,
    HttpResponseMessage response,
    bool reachedMaxRetries) : EventArgs
{
    public string Url { get; } = url;
    public int RetryCount { get; } = retryCount;
    public Exception Exception { get; } = exception;
    public HttpResponseMessage Response { get; } = response;
    public bool ReachedMaxRetries { get; } = reachedMaxRetries;
}

/// Define the event handler delegate
public delegate void RetryFailedEventHandler(object sender, RetryFailedEventArgs e);
