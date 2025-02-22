using NetTally.CustomEventArgs;

namespace NetTally.Input.Web.Handlers;

public static class RetryFailureHandler
{
    public static event RetryFailedEventHandler? RetryFailed;

    public static void OnRetryFailed(object sender, RetryFailedEventArgs e)
    {
        RetryFailed?.Invoke(sender, e);
    }
}
