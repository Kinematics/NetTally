using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace NetTally.Comparers
{
    /// <summary>
    /// A class to allow creation of custom string comparers, by specifying
    /// CompareInfo and CompareOptions during construction.
    /// </summary>
    public class CustomStringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        public CompareInfo Info { get; }
        public CompareOptions Options { get; }
        public Func<string, CompareInfo, CompareOptions, int> HashFunction { get; }

        /// <summary>
        /// Constructs a comparer using the specified CompareOptions.
        /// </summary>
        /// <param name="info">CompareInfo to use.</param>
        /// <param name="options">CompareOptions to use.</param>
        public CustomStringComparer(CompareInfo info, CompareOptions options, Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            Info = info;
            Options = options;
            HashFunction = hashFunction;
        }

        /// <summary>
        /// Compares strings with the CompareOptions specified in the constructor.
        /// Implements the generic IComparer interface.
        /// </summary>
        /// <param name="x">The first string.</param>
        /// <param name="y">The second string.</param>
        /// <returns>Returns -1 if the first string is less than the second;
        /// 1 if the first is greater than the second; and 0 if they are equal.</returns>
        public int Compare(string x, string y)
        {
            if (ReferenceEquals(x, y)) return 0;

            return Info.Compare(x, y, Options);
        }

        /// <summary>
        /// The hash code represents a number that either guarantees that two
        /// strings are different, or allows that two strings -might- be the same.
        /// Create a hash value that creates the minimal comparison possible, to
        /// see if two strings are different.
        /// </summary>
        /// <param name="str">The string to get the hash code for.</param>
        /// <returns></returns>
        public int GetHashCode(string str) => HashFunction(str, Info, Options);

        public bool Equals(string x, string y) => Compare(x, y) == 0;

        int IComparer.Compare(object x, object y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            string? xs = x as string;
            string? ys = y as string;

            if (xs is null)
                return -1;
            if (ys is null)
                return 1;
            
            return Compare(xs, ys);
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is string xx && y is string yy)
                return Compare(xx, yy) == 0;

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
