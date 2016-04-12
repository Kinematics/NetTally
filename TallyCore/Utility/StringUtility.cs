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
        /// Magic character (currently ◈) to flag a user name as a base plan.
        /// </summary>
        public static string PlanNameMarker { get; } = "\u25C8";

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
        public static IEqualityComparer<string> AgnosticStringComparer => AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant ? AgnosticStringComparer1 : AgnosticStringComparer2;

        private static IEqualityComparer<string> AgnosticStringComparer1 { get; } = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth);

        private static IEqualityComparer<string> AgnosticStringComparer2 { get; } = new CustomStringComparer(CultureInfo.InvariantCulture.CompareInfo,
            CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth);
    }

    /// <summary>
    /// A class to allow creation of custom string comparers, by specifying
    /// CompareInfo and CompareOptions during construction.
    /// </summary>
    public class CustomStringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        public CompareInfo CompareInfo { get; }
        public CompareOptions CompareOptions { get; }

        /// <summary>
        /// Constructs a comparer using the specified CompareOptions.
        /// </summary>
        /// <param name="cmpi">CompareInfo to use.</param>
        /// <param name="options">CompareOptions to use.</param>
        public CustomStringComparer(CompareInfo cmpi, CompareOptions options)
        {
            CompareInfo = cmpi;
            CompareOptions = options;
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

            return CompareInfo.Compare(x, y, CompareOptions);
        }

        public bool Equals(string x, string y) => Compare(x, y) == 0;

        /// <summary>
        /// The hash code represents a number that either guarantees that two
        /// strings are different, or allows that two strings -might- be the same.
        /// Create a hash value that creates the minimal comparison possible, to
        /// see if two strings are different.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(string obj)
        {
            if (string.IsNullOrEmpty(obj))
                return 0;

            // Different casings don't guarantee uniqueness.
            string lowered = obj.ToLowerInvariant();

            // Different accents don't guarantee uniqueness.
            // Decompose a unicode string so that 'accented' charaters are broken
            // into their original + accent marks as separate character entities.
            // EG: á becomes a + ́
            string decomposed = lowered.Normalize(System.Text.NormalizationForm.FormD);

            // Different spacing/punctuation doesn't guarantee uniqueness.
            var decArr = decomposed.ToCharArray();
            var filtered = decArr.Where(c => char.IsLetterOrDigit(c));

            // Convert the LINQ results to a new string.
            string check = new string(filtered.ToArray());

            // And if this hash code comes out different, we can be certain that
            // two strings that are compared will be different.
            return check.GetHashCode();
        }

        int IComparer.Compare(object x, object y) => Compare(x as string, y as string);

        bool IEqualityComparer.Equals(object x, object y) => ((IComparer)this).Compare(x, y) == 0;

        int IEqualityComparer.GetHashCode(object obj) => this.GetHashCode(obj as string);
    }
}
