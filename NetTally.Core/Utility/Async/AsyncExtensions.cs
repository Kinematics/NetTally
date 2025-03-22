using System.Diagnostics.CodeAnalysis;

namespace NetTally.Utility.Async;

/// <summary>
/// Class for extension methods on async methods or values.
/// </summary>
static class AsyncExtensions
{
    /// <summary>
    /// Function to allow setting a timeout on an async function that doesn't natively permit it.
    /// From: http://stackoverflow.com/a/22078975/770213
    /// </summary>
    /// <typeparam name="TResult">The return type of the task being awaited.</typeparam>
    /// <param name="task">The task to await.</param>
    /// <param name="timeout">The timeout to wait for the task to complete.</param>
    /// <param name="token">A cancellation token for user-initiated cancellations.</param>
    /// <returns>Returns the awaited task, if it completed in less than the timeout period.</returns>
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token)
    {
        using CancellationTokenSource timeoutTokenSource = new(timeout);
        using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token);

        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, linkedTokenSource.Token));

        if (completedTask == task)
        {
            return await task;  // Very important in order to propagate exceptions
        }

        token.ThrowIfCancellationRequested();

        throw new TimeoutException("The operation has timed out.");
    }


    /*

    Define async Try-method like this:

        public async Task<(bool, string)> TryReceiveAsync()
        {
            string message;
            bool success;
            // ...
            return (success, message);
        }

    Call the async Try-method like this:

        if ((await TryReceiveAsync()).TryOut(out string msg))
        {
            // use msg
        }

    */

    /// <summary>
    /// Use a try pattern to assign an out parameter while returning
    /// a bool to indicate whether the attempt succeeded, for async
    /// methods.
    /// </summary>
    /// <typeparam name="P2">The type of object put in the out parameter.</typeparam>
    /// <param name="tuple">The return tuple of the original method.</param>
    /// <param name="p2">The parameter being returned.</param>
    /// <returns>Returns <c>true</c> if the function succeeded, and p2 is valid.</returns>
    public static bool TryOut<P2>(this ValueTuple<bool, P2> tuple,
        [NotNullWhen(true)] out P2 p2)
    {
        bool p1;
        (p1, p2) = tuple;
        return p1;
    }

    public static bool TryOut<P2, P3>(this ValueTuple<bool, P2, P3> tuple,
        [MaybeNullWhen(false)] out P2 p2, [MaybeNullWhen(false)] out P3 p3)
    {
        bool p1;
        (p1, p2, p3) = tuple;
        return p1;
    }

}
