using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace NetTally.Utility
{
    public static class Agnostic
    {
        internal static class DefaultAgnosticHashFunction
        {
            /// <summary>
            /// Always return 0, since we can't assume that we'll have access to UTF comparer functions,
            /// which means we can't properly distinguish between different types of graphemes.
            /// </summary>
            public static int HashFunction(string str, CompareInfo info, CompareOptions options)
            {
                return 0;
            }
        }

        private static IEqualityComparer<string> currentComparer;

        private static IEqualityComparer<string> StringComparer1 { get; set; }

        private static IEqualityComparer<string> StringComparer2 { get; set; }

        /// <summary>
        /// Initialize the agnostic string comparers using the provided hash function.
        /// Injects the function from the non-PCL assembly, to get around PCL limitations.
        /// MUST be run before other objects are constructed.
        /// </summary>
        /// <param name="hashFunction"></param>
        public static void HashStringsUsing(Func<string, CompareInfo, CompareOptions, int> hashFunction)
        {
            hashFunction = hashFunction ?? DefaultAgnosticHashFunction.HashFunction;

            StringComparer1 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);
            StringComparer2 = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
                CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth, hashFunction);

            AdvancedOptions.Instance.PropertyChanged += AdvancedOptions_PropertyChanged;

            currentComparer = AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant ? StringComparer1 : StringComparer2;
        }

        private static void AdvancedOptions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WhitespaceAndPunctuationIsSignificant")
            {
                currentComparer = AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant ? StringComparer1 : StringComparer2;
            }
        }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static IEqualityComparer<string> StringComparer =>
            currentComparer ?? throw new InvalidOperationException("Agnostic string comparers have not been initialized.");
    }
}
