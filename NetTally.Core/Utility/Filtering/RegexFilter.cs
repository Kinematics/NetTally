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
    /// <typeparam name="T"></typeparam>
    public class RegexFilter<T> : IItemFilter<T>
    {
        readonly List<RegexPattern> patterns = new List<RegexPattern>();
        readonly Func<T, string> map;

        public RegexFilter(Func<T, string> map, params Regex[] regexes)
            : this(map, regexes.Select(r => new RegexPattern(r)).ToArray())
        {
        }

        public RegexFilter(Func<T, string> map, params RegexPattern[] patterns)
        {
            this.patterns.AddRange(patterns);
            this.map = map;
        }

        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(T item)
        {
            return patterns.Any(r => r.IsMatch(map(item)));
        }
    }
}
