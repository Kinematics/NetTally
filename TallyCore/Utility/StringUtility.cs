using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Utility
{
    /// <summary>
    /// Class for general static functions relating to text manipulation and comparisons.
    /// </summary>
    public static class StringUtility
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Filter unsafe characters from the provided string.
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>The input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string SafeString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return UnsafeCharsRegex.Replace(input, "");
        }

        /// <summary>
        /// Magic character (currently ◈, \u25C8) to flag a user name as a base plan.
        /// </summary>
        public static string PlanNameMarker { get; } = "◈";

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static bool IsPlanName(string name) => name?.StartsWith(PlanNameMarker, StringComparison.Ordinal) ?? false;

        /// <summary>
        /// Takes an input string that is potentially composed of multiple text lines,
        /// and splits it up into a List of strings of one text line each.
        /// Does not generate empty lines.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>The list of all string lines in the input.</returns>
        public static List<string> GetStringLines(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new List<string>();

            string[] split = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(split);
        }

        /// <summary>
        /// Get the first line (pre-EOL) of a potentially multi-line string.
        /// </summary>
        /// <param name="input">The string to get the first line from.</param>
        /// <returns>Returns the first line of the provided string.</returns>
        public static string GetFirstLine(string input)
        {
            var lines = GetStringLines(input);
            return lines.FirstOrDefault();
        }

        /// <summary>
        /// A string comparer object that allows comparison between strings that
        /// can ignore lots of annoying user-entered variances.
        /// </summary>
        public static IEqualityComparer<string> AgnosticStringComparer
        {
            get {
                var comparer = AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant ? AgnosticStringComparer1 : AgnosticStringComparer2;
                if (comparer == null)
                    throw new InvalidOperationException("Agnostic string comparers have not been initialized.");
                return comparer;
            }
        }

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

        private static IEqualityComparer<string> AgnosticStringComparer1 { get; set; }

        private static IEqualityComparer<string> AgnosticStringComparer2 { get; set; }
    }

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
            if (ReferenceEquals(x, null)) return -1;
            if (ReferenceEquals(y, null)) return 1;

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

        int IComparer.Compare(object x, object y) => Compare(x as string, y as string);

        bool IEqualityComparer.Equals(object x, object y) => Compare(x as string, y as string) == 0;

        int IEqualityComparer.GetHashCode(object obj) => GetHashCode(obj as string);
    }
}
