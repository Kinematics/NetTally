using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NetTally.Utility
{

    public static class Text
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Filter unsafe characters from the provided string.
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>The input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string SafeString(string input)
        {
            if (input == null)
                return "";

            return UnsafeCharsRegex.Replace(input, "");
        }

        /// <summary>
        /// Magic character (currently ◈) to flag a user name as a base plan.
        /// </summary>
        public static string PlanNameMarker { get; } = "\u25C8";

        /// <summary>
        /// Get the first line (pre-EOL) of a potentially multi-line string.
        /// </summary>
        /// <param name="multiLine">The string to get the first line from.</param>
        /// <returns>Returns the first line of the provided string.</returns>
        public static string FirstLine(string multiLine)
        {
            int i = multiLine.IndexOf("\r");
            if (i > 0)
                return multiLine.Substring(i);
            else
                return multiLine;
        }
    }

    /// <summary>
    /// Custom sorting class for sorting votes.
    /// Sorts by Task+Content.
    /// </summary>
    public class CustomVoteSort : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));

            string xs = x as string;
            if (xs == null)
                throw new ArgumentException("Parameter x is not a string.");

            string ys = y as string;
            if (ys == null)
                throw new ArgumentException("Parameter x is not a string.");

            string compX = VoteString.GetVoteTask(xs) + " " + VoteString.GetVoteContent(xs);
            string compY = VoteString.GetVoteTask(ys) + " " + VoteString.GetVoteContent(ys);

            int result = string.Compare(compX, compY, CultureInfo.CurrentUICulture, CompareOptions.IgnoreCase);

            return result;
        }
    }

    public static class MathUtil
    {
        public static T BoundsCheck<T>(T? min, T value, T? max) where T: struct, IComparable<T>
        {
            if (min.HasValue && value.CompareTo(min.Value) < 0)
                return min.Value;

            if (max.HasValue && value.CompareTo(max.Value) > 0)
                return max.Value;

            return value;
        }
    }
}
