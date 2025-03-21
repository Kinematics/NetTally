namespace NetTally.Utility.Events;

/// Define the event handler delegate
public delegate void RetryFailedEventHandler(object sender, RetryFailedEventArgs e);

public static class RetryFailureHandler
{
    public static event RetryFailedEventHandler? RetryFailed;

    public static void OnRetryFailed(object sender, RetryFailedEventArgs e)
    {
        RetryFailed?.Invoke(sender, e);
    }
}
