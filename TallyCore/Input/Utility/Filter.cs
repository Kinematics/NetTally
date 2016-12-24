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

        /// <summary>
        /// Filter constructor.  Create a filter based on an explicit regex.
        /// </summary>
        /// <param name="regex">An explicit regex to use for filtering.</param>
        public Filter(Regex regex)
        {
            filterRegex = regex;
        }

        /// <summary>
        /// Public builder function.  Creates a regex from the provided input strings.
        /// If it's flagged as isRegex, treats the strings as explicit regex values.
        /// Otherwise treats them as ordinary strings, with comma-separated options
        /// and optional splat globbing.
        /// </summary>
        /// <param name="filterString">The user-defined filter string.</param>
        /// <param name="defaultString">The default, program-provided string to filter on.</param>
        /// <param name="isRegex">If this flag is set, treat the provided strings as pre-defined regex strings.</param>
        public Filter(string filterString, string defaultString, bool isRegex)
        {
            if (isRegex)
            {
                filterRegex = CreateFullRegex(filterString, defaultString);
            }
            else
            {
                filterRegex = CreateSimpleRegex(filterString, defaultString);
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
        /// Creates a regex based on the provided strings.
        /// Treats these strings as already-constructed regex values.
        /// </summary>
        /// <param name="regexString">The user-defined regex string.</param>
        /// <param name="defaultString">The default, program-provided string to filter on.</param>
        /// <returns>Returns a regex constructed from the strings.</returns>
        private static Regex CreateFullRegex(string regexString, string defaultString)
        {
            if (string.IsNullOrEmpty(regexString) && string.IsNullOrEmpty(defaultString))
                return new Regex(EmptyLine);

            string safeRegexString = regexString.RemoveUnsafeCharacters();

            if (string.IsNullOrEmpty(defaultString))
            {
                return new Regex(safeRegexString, RegexOptions.IgnoreCase);
            }
            else
            {
                if (string.IsNullOrEmpty(safeRegexString))
                    return new Regex(defaultString, RegexOptions.IgnoreCase);
                else
                    return new Regex($"{safeRegexString}|{defaultString}", RegexOptions.IgnoreCase);
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
