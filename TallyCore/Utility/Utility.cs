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

            string compX = VoteLine.GetVoteTask(xs) + " " + VoteLine.GetVoteContent(xs);
            string compY = VoteLine.GetVoteTask(ys) + " " + VoteLine.GetVoteContent(ys);

            int result = string.Compare(compX, compY, CultureInfo.CurrentUICulture, CompareOptions.IgnoreCase);

            return result;
        }
    }

}
