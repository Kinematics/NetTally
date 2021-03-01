using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering
{
    /// <summary>
    /// An item filter that determines whether an object is allowed by
    /// running a regex test against a string extraction of the object.
    /// </summary>
    public class RegexFilter : IItemFilter<string>
    {
        readonly List<RegexPattern> patterns = new List<RegexPattern>();

        /// <summary>
        /// Construct a new regex filter using the provided regex objects.
        /// </summary>
        /// <param name="regexes"></param>
        public RegexFilter(params Regex[] regexes)
            : this(regexes.Select(r => new RegexPattern(r)).ToArray())
        {
        }

        /// <summary>
        /// Construct a new regex filter using the provided regex patterns.
        /// </summary>
        /// <param name="patterns"></param>
        public RegexFilter(params RegexPattern[] patterns)
        {
            this.patterns.AddRange(patterns);
        }

        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(string item)
        {
            return patterns.Any(a => a.IsMatch(item));
        }

        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <typeparam name="U">The type of object being passed in.</typeparam>
        /// <param name="item">The item being checked.</param>
        /// <param name="map">A function that maps a U to a string.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows<U>(U item, Func<U, string> map)
        {
            return Allows(map(item));
        }
    }
}
