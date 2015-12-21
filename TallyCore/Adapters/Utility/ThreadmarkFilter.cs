using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Adapters
{
    public class ThreadmarkFilter
    {
        static readonly Regex omakeRegex = new Regex(@"\bomake\b", RegexOptions.IgnoreCase);

        bool useCustomRegex = false;
        Regex customRegex = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="quest">Quest to use as a basis for constructing a custom filter regex.</param>
        public ThreadmarkFilter(IQuest quest)
        {
            if (quest != null)
            {
                useCustomRegex = quest.UseCustomThreadmarkFilters;

                if (useCustomRegex)
                {
                    CreateCustomRegex(quest.CustomThreadmarkFilters);
                }
            }
        }

        /// <summary>
        /// Create a custom regex from the provided filter string.
        /// </summary>
        /// <param name="customThreadmarkFilters">A string with a comma-delimited
        /// list of words that can be used to create a custom threadmark filter.</param>
        private void CreateCustomRegex(string customThreadmarkFilters)
        {
            if (string.IsNullOrEmpty(customThreadmarkFilters))
                return;

            string safeCustomThreadmarkFilters = Utility.StringUtility.SafeString(customThreadmarkFilters);

            var splits = safeCustomThreadmarkFilters.Split(',');

            var options = splits.Aggregate((s, t) => s.Trim() + "|" + t.Trim());

            string rString = $@"\b({options})\b";

            try
            {
                customRegex = new Regex(rString, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                customRegex = null;
            }
        }

        /// <summary>
        /// Filter the provided title string based on either the default or
        /// the custom filter regex.
        /// </summary>
        /// <param name="title">The string to test.</param>
        /// <returns>Returns true if the provided string matches the currently active filter.</returns>
        public bool Filter(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            if (useCustomRegex && customRegex != null)
                return customRegex.Match(title).Success;

            return omakeRegex.Match(title).Success;
        }
    }
}
