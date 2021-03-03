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
        protected readonly FilterType filterType;
        protected readonly List<RegexPattern> patterns = new List<RegexPattern>();

        /// <summary>
        /// Construct a new regex filter using the provided regex objects.
        /// </summary>
        /// <param name="regexes"></param>
        //public RegexFilter(FilterType filterType, params Regex[] regexes)
        //    : this(filterType, regexes.Select(r => new RegexPattern(r)).ToArray())
        //{
        //}

        /// <summary>
        /// Construct a new regex filter using the provided regex patterns.
        /// </summary>
        /// <param name="patterns"></param>
        protected RegexFilter(FilterType filterType, IEnumerable<RegexPattern> patterns)
        {
            this.patterns.AddRange(patterns);
            this.filterType = filterType;
        }


        #region Factories used to construct varying types of list filters.
        public static RegexFilter Allow(RegexPattern pattern, params RegexPattern[] patterns)
        {
            var all = new List<RegexPattern> { pattern };
            all.AddRange(patterns);

            return new RegexFilter(FilterType.Allow, all);
        }
        public static RegexFilter Block(RegexPattern pattern, params RegexPattern[] patterns)
        {
            var all = new List<RegexPattern> { pattern };
            all.AddRange(patterns);

            return new RegexFilter(FilterType.Block, all);
        }

        public static RegexFilter Allow(Regex regex, params Regex[] regexes)
        {
            var all = new List<Regex> { regex };
            all.AddRange(regexes);


            return new RegexFilter(FilterType.Allow, all.Select(p => new RegexPattern(p)));
        }
        public static RegexFilter Block(Regex regex, params Regex[] regexes)
        {
            var all = new List<Regex> { regex };
            all.AddRange(regexes);


            return new RegexFilter(FilterType.Block, all.Select(p => new RegexPattern(p)));
        }

        public static readonly RegexFilter AllowAll = new(FilterType.Block, Enumerable.Empty<RegexPattern>());
        public static readonly RegexFilter BlockAll = new(FilterType.Allow, Enumerable.Empty<RegexPattern>());
        #endregion


        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(string item)
        {
            return filterType switch
            {
                FilterType.Allow => patterns.Any(a => a.IsMatch(item)),
                FilterType.Block => !patterns.Any(a => a.IsMatch(item)),
                _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
            };
        }

        public bool Blocks(string item)
        {
            return filterType switch
            {
                FilterType.Allow => !patterns.Any(a => a.IsMatch(item)),
                FilterType.Block => patterns.Any(a => a.IsMatch(item)),
                _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
            };
        }
    }
}
