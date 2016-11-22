using System;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally.Filters
{
    public class BaseFilter
    {
        const string emptyLine = "^$";
        readonly Regex defaultRegex;
        readonly Regex customRegex;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="defaultRegex">The default regex for the filter to use.</param>
        /// <param name="defaultRegex">A custom regex that the filter may use instead of the default.</param>
        public BaseFilter(Regex defaultRegex, Regex customRegex)
        {
            this.defaultRegex = defaultRegex;
            this.customRegex = customRegex;
        }

        /// <summary>
        /// Create a custom regex from the provided filter string.
        /// Commas are considered separators for 'or' testing.
        /// </summary>
        /// <param name="customFilterString">A string with a comma-delimited
        /// list of words that can be used to create a custom filter.</param>
        protected static Regex CreateCustomRegex(string customFilterString, bool allowBlankLines)
        {
            if (customFilterString == null)
                return null;

            string safeCustomFilters = Utility.StringUtility.SafeString(customFilterString);

            var splits = safeCustomFilters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            string bar = "";

            foreach (var split in splits)
            {
                string s = split.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    sb.Append($"{bar}{s}");
                    bar = "|";
                }
            }

            string sbString = sb.ToString();

            string rString;

            if (string.IsNullOrEmpty(sbString))
                if (allowBlankLines)
                    rString = emptyLine;
                else
                    return null;
            else
                rString = $@"\b({sbString})\b";

            try
            {
                return new Regex(rString, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Filter the provided title string based on either the default or
        /// the custom filter regex.
        /// </summary>
        /// <param name="test">The string to test.</param>
        /// <returns>
        /// Returns true if the provided string matches the current (or default) filter.
        /// Always returns false if the test string is null.
        /// If the custom and default filters are null, returns true.</returns>
        public bool Filter(string test)
        {
            // Test string should never be null.  If it is, return false.
            if (test == null)
                return false;

            return customRegex?.Match(test).Success ?? defaultRegex?.Match(test).Success ?? true;
        }
    }
}
