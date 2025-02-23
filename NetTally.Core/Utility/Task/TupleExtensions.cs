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
