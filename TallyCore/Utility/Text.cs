using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Takes an input string that is potentially composed of multiple text lines,
        /// and splits it up into a List of strings of one text line each.
        /// Does not generate empty lines.
        /// </summary>
        /// <param name="input">The input text.</param>
        /// <returns>The list of all string lines in the input.</returns>
        public static List<string> GetStringLines(string input)
        {
            string[] split = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return new List<string>(split);
        }

        /// <summary>
        /// Get the first line (pre-EOL) of a potentially multi-line string.
        /// </summary>
        /// <param name="input">The string to get the first line from.</param>
        /// <returns>Returns the first line of the provided string.</returns>
        public static string FirstLine(string input)
        {
            var lines = GetStringLines(input);
            return lines.FirstOrDefault();
        }
    }
}
