using System;
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
        /// <summary>
        /// Regex for control and formatting characters that we don't want to allow processing of.
        /// EG: \u200B, non-breaking space
        /// Regex is the character set of all control characters {C}, except for CR/LF.
        /// </summary>
        static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Remove unsafe UTF control characters from the provided string.
        /// Returns an empty string if given null.
        /// </summary>
        /// <param name="input">Any string.</param>
        /// <returns>Returns the input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string RemoveUnsafeCharacters(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return UnsafeCharsRegex.Replace(input, "");
        }

        /// <summary>
        /// Magic character (currently ◈, \u25C8) to mark a named voter as a plan rather than a user.
        /// </summary>
        public static string PlanNameMarker { get; } = "◈";

        /// <summary>
        /// Check if the provided name starts with the plan name marker.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>Returns true if the name starts with the plan name marker.</returns>
        public static bool IsPlanName(this string name) => name?.StartsWith(PlanNameMarker, StringComparison.Ordinal) ?? false;

        /// <summary>
        /// Static array for use in GetStringLines.
        /// </summary>
        static char[] newLines = new[] { '\r', '\n' };

        /// <summary>
        /// Takes an input string that is potentially composed of multiple text lines,
        /// and splits it up into a List of strings of one text line each.
        /// Does not generate empty lines.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>The list of all string lines in the input.</returns>
        public static List<string> GetStringLines(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return new List<string>();

            string[] split = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);

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
}
