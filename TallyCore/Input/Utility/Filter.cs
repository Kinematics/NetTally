using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Filters
{
    /// <summary>
    /// Class to handle user-defined filters for different elements of a tally.
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
        /// <param name="filterRegex">An explicit regex to use for filtering.</param>
        public Filter(Regex filterRegex)
        {
            this.filterRegex = filterRegex;
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

        /// <summary>
        /// Function to create a regex based on a user-provided string of values to check for.
        /// User-provided string is assumed to be comma-delimited, but may be a full regex
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

            // Add the default string value at the end.
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

    }
}
