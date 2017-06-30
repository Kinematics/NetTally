using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Extensions
{
    /// <summary>
    /// Class to hold extension methods for various types of string utility work.
    /// </summary>
    public static class StringExtensions
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
            var result = new List<string>();

            if (!string.IsNullOrEmpty(input))
            {
                string[] split = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
                result.AddRange(split);
            }

            return result;
        }

        /// <summary>
        /// Get the first line (pre-EOL) of a potentially multi-line string.
        /// </summary>
        /// <param name="input">The string to get the first line from.</param>
        /// <returns>Returns the first line of the provided string.</returns>
        public static string GetFirstLine(this string input)
        {
            var lines = GetStringLines(input);
            return lines.FirstOrDefault();
        }

        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends the enumerable.
        /// </summary>
        /// <param name="self">The list to search.</param>
        /// <param name="value">The value to compare with.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this IEnumerable<string> self, string value)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            foreach (string item in self)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Returns the first match within the enumerable list that agnostically
        /// equals the provided value.
        /// Extends a string.
        /// </summary>
        /// <param name="value">The value to compare with.</param>
        /// <param name="list">The list to search.</param>
        /// <returns>Returns the item in the list that matches the value, or null.</returns>
        public static string AgnosticMatch(this string value, IEnumerable<string> list)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            foreach (string item in list)
            {
                if (Agnostic.StringComparer.Equals(item, value))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Find the first character difference between two strings.
        /// </summary>
        /// <param name="first">First string.</param>
        /// <param name="second">Second string.</param>
        /// <returns>Returns the index of the first difference between the strings.  -1 if they're equal.</returns>
        public static int FirstDifferenceInStrings(this string first, string second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));

            int length = first.Length < second.Length ? first.Length : second.Length;

            for (int i = 0; i < length; i++)
            {
                if (first[i] != second[i])
                    return i;
            }

            if (first.Length != second.Length)
                return Math.Min(first.Length, second.Length);

            return -1;
        }
    }
}
