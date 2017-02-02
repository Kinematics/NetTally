using System;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Extensions;

namespace NetTally.Utility
{
    /// <summary>
    /// Class to handle user-defined filters, to be used against text input.
    /// Example cases are for filtering threadmarks and tasks.
    /// </summary>
    public class Filter
    {
        readonly Regex filterRegex;
        public const string EmptyLine = "^$";
        static readonly Regex escapeChars = new Regex(@"([.?(){}^$\[\]])");
        static readonly Regex splat = new Regex(@"\*");
        static readonly Regex preWord = new Regex(@"^\w");
        static readonly Regex postWord = new Regex(@"\w$");

        static readonly Regex jsRegex = new Regex(@"^/(?<regex>.+)/(?<options>[ugi]{0,3})$");

        /// <summary>
        /// Filter constructor.  Create a filter based on an explicit regex.
        /// </summary>
        /// <param name="regex">An explicit regex to use for filtering.</param>
        public Filter(Regex regex)
        {
            filterRegex = regex;
        }

        /// <summary>
        /// Public constructor.  Creates a regex from the provided input strings.
        /// If it's flagged as isRegex, treats the strings as explicit regex values.
        /// Otherwise treats them as ordinary strings, with comma-separated options
        /// and optional splat globbing.
        /// </summary>
        /// <param name="filterString">The user-defined filter string.</param>
        /// <param name="defaultString">The default, program-provided string to filter on.</param>
        public Filter(string filterString, string defaultString)
        {
            filterRegex = CreateRegex(filterString, defaultString);
        }

        /// <summary>
        /// Creates and returns a regex appropriate to the provided filter strings.
        /// </summary>
        /// <param name="filterString">User-defined filter string.</param>
        /// <param name="defaultString">Default filter string for the filter.</param>
        /// <returns>Returns a <see cref="Regex"/> based on the properties of the provided
        /// strings.</returns>
        private Regex CreateRegex(string filterString, string defaultString)
        {
            string userString = filterString.RemoveUnsafeCharacters().Trim();

            if (IsJSRegex(userString, out string jsRegexString))
            {
                return CreateDefinedRegex(jsRegexString, defaultString);
            }
            else
            {
                return CreateSimpleRegex(userString, defaultString);
            }
        }

        /// <summary>
        /// Test whether the supplied string is formatted as a javascript regex.
        /// EG: /some text/i
        /// If so, treat the provided filter string as a regex rather than a string literal.
        /// </summary>
        /// <param name="filterString">The filter string to test.</param>
        /// <param name="jsRegexString">The regex portion of the string, if found.</param>
        /// <returns>Returns true (and sets jsRegexString to the regex contents) if it determined
        /// that the provided string was formatted as a javascript regex. Otherwise false and null.</returns>
        private bool IsJSRegex(string filterString, out string jsRegexString)
        {
            jsRegexString = null;

            if (string.IsNullOrEmpty(filterString))
                return false;

            Match m = jsRegex.Match(filterString);

            if (m.Success)
                jsRegexString = m.Groups["regex"].Value;

            return m.Success;
        }

        /// <summary>
        /// Creates a regex based on the provided strings.
        /// Assumes the strings are already in a regex-like format.
        /// </summary>
        /// <param name="jsRegexString">The user-provided regex string. Must not be null or empty.</param>
        /// <param name="defaultString">The default filter value for the filter.</param>
        /// <returns>Returns a regex that combines the user-provided string with the default string.</returns>
        /// <exception cref="ArgumentNullException"/>
        private Regex CreateDefinedRegex(string jsRegexString, string defaultString)
        {
            if (string.IsNullOrEmpty(jsRegexString))
                throw new ArgumentNullException(nameof(jsRegexString));

            if (string.IsNullOrEmpty(defaultString))
            {
                return new Regex(jsRegexString, RegexOptions.IgnoreCase);
            }
            else
            {
                if (jsRegexString.Contains(defaultString))
                    return new Regex(jsRegexString, RegexOptions.IgnoreCase);
                else
                    return new Regex($"{jsRegexString}|{defaultString}", RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Creates a regex based on the provided strings.
        /// Treats these strings as simple, comma-delimited option lists,
        /// with possible * globs.
        /// </summary>
        /// <param name="simpleString">The user-defined filter string.</param>
        /// <param name="defaultString">The default, program-provided string to filter on.</param>
        /// <returns>Returns a regex constructed from the strings.</returns>
        private static Regex CreateSimpleRegex(string simpleString, string defaultString)
        {
            if (string.IsNullOrEmpty(simpleString) && string.IsNullOrEmpty(defaultString))
                return new Regex(EmptyLine);

            string safeGlobString = simpleString.RemoveUnsafeCharacters();

            var splits = safeGlobString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            string bar = "";

            // Convert comma-separated options to | regex options.
            // If a segment contains special regex characters, escape those characters.
            // If a segment contains a *, treat it as a general glob: .*
            foreach (var split in splits)
            {
                string s = split.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    s = escapeChars.Replace(s, @"\$1");
                    s = splat.Replace(s, @".*");

                    string preBound = "";
                    string postBound = "";
                    if (preWord.Match(s).Success)
                        preBound = @"\b";
                    if (postWord.Match(s).Success)
                        postBound = @"\b";

                    sb.Append($"{bar}{preBound}{s}{postBound}");
                    bar = "|";
                }
            }

            // Add the default string value (if any) at the end.
            if (!string.IsNullOrEmpty(defaultString))
                sb.Append($"{bar}{defaultString}");

            try
            {
                return new Regex($@"{sb.ToString()}", RegexOptions.IgnoreCase);
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
