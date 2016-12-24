using System;
using System.Collections.Generic;
using System.Globalization;

namespace NetTally.Utility
{
    public static class Agnostic
    {
        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction"></param>
        public static void InitStringComparers(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            AgnosticStringComparer1 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            AgnosticStringComparer2 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
        }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static IEqualityComparer<string> AgnosticStringComparer
        {
            get
            {
                var comparer = AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant ? AgnosticStringComparer1 : AgnosticStringComparer2;
                if (comparer == null)
                    throw new InvalidOperationException("Agnostic string comparers have not been initialized.");
                return comparer;
            }
        }

        private static IEqualityComparer<string> AgnosticStringComparer1 { get; set; }

        private static IEqualityComparer<string> AgnosticStringComparer2 { get; set; }
    }
}
