using System.Diagnostics.CodeAnalysis;

namespace NetTally.Utility.Task;

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

public static class TupleExtensions
{
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
