namespace NetTally.Utility.Events;

public static class RetryFailureHandler
{
    public static event RetryFailedEventHandler? RetryFailed;

    public static void OnRetryFailed(object sender, RetryFailedEventArgs e)
    {
        RetryFailed?.Invoke(sender, e);
    }
}
