using System.Collections;
using System.Globalization;

namespace NetTally.Utility.Comparers
{
    /// <summary>
    /// A class to allow creation of custom string comparers, by specifying
    /// CompareInfo and CompareOptions during construction.
    /// </summary>
    /// <remarks>
    /// Constructs a comparer using the specified CompareOptions.
    /// </remarks>
    /// <param name="info">CompareInfo to use.</param>
    /// <param name="options">CompareOptions to use.</param>
    /// <param name="hashFunction">Hash function to use.</param>
    public class AgnosticStringComparer(
        CompareInfo info,
        CompareOptions options,
        Func<string, CompareInfo, CompareOptions, int> hashFunction)
        : StringComparer, IComparer, IComparer<string>,
          IEqualityComparer, IEqualityComparer<string>
    {
        public CompareInfo Info { get; } = info;
        public CompareOptions Options { get; } = options;
        public Func<string, CompareInfo, CompareOptions, int> HashFunction { get; } = hashFunction;

        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;

            return Info.Compare(x, y, Options);
        }

        public override bool Equals(string? x, string? y) =>
            Compare(x, y) == 0;

        public override int GetHashCode(string str) => Info.GetHashCode(str, Options);

        int IComparer.Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is not string xs)
                return -1;
            if (y is not string ys)
                return 1;

            return Compare(xs, ys);
        }

        bool IEqualityComparer.Equals(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is string xs && y is string ys)
                return Compare(xs, ys) == 0;

            return false;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            if (obj is string str)
                return GetHashCode(str);

            return 0;
        }
    }
}
