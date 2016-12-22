using System;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Utility
{
    /// <summary>
    /// Class to handle user-defined filters, to be used against text input.
    /// Example cases are for filtering threadmarks and tasks.
    /// </summary>
    public class Filter
    {
        public const string EmptyLine = "^$";
        readonly Regex filterRegex;

        /// <summary>
        /// Filter constructor.
        /// </summary>
        /// <param name="filterString">The primary, user-defined string to filter on.</param>
        /// <param name="defaultString">The default, program-provided string to filter on.</param>
        public Filter(string filterString, string defaultString)
        {
            filterRegex = CreateRegex(filterString, defaultString);
        }

        /// <summary>
        /// Filter constructor.
        /// </summary>
        /// <param name="inputRegex">An explicit regex to use for filtering.</param>
        public Filter(Regex inputRegex)
        {
            filterRegex = inputRegex;
        }

        /// <summary>
        /// Function to create a regex based on a user-provided string of values to check for.
        /// The user-provided string is assumed to be comma-delimited, but may be a full regex
        /// by itself (as long as it doesn't have any commas).
        /// If both the primary and default test strings are empty, uses the EmptyLine for the
        /// regex instead.
        /// </summary>
        /// <param name="filterString">The primary set of values for the regex.</param>
        /// <param name="defaultString">The default/backup values to test for.</param>
        /// <returns>Returns the constructed regex.</returns>
        public static Regex CreateRegex(string filterString, string defaultString)
        {
            if (string.IsNullOrEmpty(filterString) && string.IsNullOrEmpty(defaultString))
                return new Regex(EmptyLine);

            string safeFilterString = filterString.RemoveUnsafeCharacters();

            var splits = safeFilterString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            string bar = "";

            // Convert comma-separated options to | regex options.
            foreach (var split in splits)
            {
                string s = split.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    sb.Append($"{bar}{s}");
                    bar = "|";
                }
            }

            // Add the default string value (if any) at the end.
            if (!string.IsNullOrEmpty(defaultString))
                sb.Append($"{bar}{defaultString}");

            string sbString = sb.ToString();

            try
            {
                return new Regex($@"\b({sbString})\b", RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Function to test whether a provided string is matched by the current filter.
        /// </summary>
        /// <param name="input">The string to check against the filter.</param>
        /// <returns>Returns true if the filter matches some part of the input string.</returns>
        public bool Match(string input)
        {
            if (filterRegex == null)
                return false;

            return filterRegex.Match(input).Success;
        }
    }
}
