using System;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Input.Utility
{
    /// <summary>
    /// Class to handle user-defined filters, to be used against text input.
    /// Example cases are for filtering threadmarks and tasks.
    /// </summary>
    public class Filter
    {
        #region Public Static
        const string OmakeFilter = @"\bomake\b";
        public static readonly Filter DefaultThreadmarkFilter = new Filter(OmakeFilter, null);
        #endregion

        #region Class Fields
        readonly Regex filterRegex;

        static readonly Regex EmptyRegex = new Regex("^$");
        public static readonly Filter Empty = new Filter(EmptyRegex);

        static readonly Regex escapeChars = new Regex(@"([.?(){}^$\[\]])");
        static readonly Regex splat = new Regex(@"\*");
        static readonly Regex preWord = new Regex(@"^\w");
        static readonly Regex postWord = new Regex(@"\w$");

        static readonly Regex jsRegex = new Regex(@"^/(?<regex>.+)/(?<options>[ugi]{0,3})$");

        /// <summary>
        /// A pure false regex, in as simple a form as possible.  From the start of the line,
        /// require a negative lookahead for a value that is followed by that value.
        /// </summary>
        static readonly Regex alwaysFalse = new Regex(@"^(?!x)x");
        #endregion

        #region Constructors
        /// <summary>
        /// Create a filter using an explicit regex.
        /// </summary>
        /// <param name="regex">An explicit regex to use for filtering.
        /// If null is passed, use the alwaysFalse regex.</param>
        public Filter(Regex regex)
        {
            filterRegex = regex ?? alwaysFalse;
        }

        /// <summary>
        /// Public constructor.  Creates a regex from the provided input strings.
        /// </summary>
        /// <param name="filterString">The user-defined filter string.</param>
        /// <param name="injectString">An extra (program-provided) string to inject into the filter string.</param>
        public Filter(string filterString, string injectString)
        {
            filterString = filterString ?? string.Empty;
            filterRegex = CreateRegex(filterString, injectString);
        }
        #endregion

        #region Create regex functions
        /// <summary>
        /// Creates and returns a regex appropriate to the provided filter strings.
        /// </summary>
        /// <param name="filterString">User-defined filter string.</param>
        /// <param name="injectString">Default filter string for the filter.</param>
        /// <returns>Returns a <see cref="Regex"/> based on the properties of the provided
        /// strings.</returns>
        private Regex CreateRegex(string filterString, string injectString)
        {
            string userString = filterString.RemoveUnsafeCharacters().Trim();

            // Check for !, indicating the filter should be inverted.
            if (!string.IsNullOrEmpty(userString) && userString[0] == '!')
            {
                IsInverted = true;
                userString = userString.Substring(1).Trim();
            }

            if (IsJSRegex(userString, out string jsRegexString))
            {
                return CreateDefinedRegex(jsRegexString, injectString);
            }
            else
            {
                return CreateSimpleRegex(userString, injectString);
            }
        }

        /// <summary>
        /// Test whether the supplied string is formatted as a javascript regex.
        /// EG: /some text/i
        /// If so, treat the provided filter string as a regex rather than a simple string literal.
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
        /// <param name="injectString">An optional additonal value to insert into the regex.</param>
        /// <returns>Returns a regex that combines the user-provided string with the injected string.</returns>
        /// <exception cref="ArgumentNullException"/>
        private Regex CreateDefinedRegex(string jsRegexString, string injectString)
        {
            if (string.IsNullOrEmpty(jsRegexString))
                throw new ArgumentNullException(nameof(jsRegexString));

            if (string.IsNullOrEmpty(injectString))
            {
                return new Regex(jsRegexString, RegexOptions.IgnoreCase);
            }
            else
            {
                return new Regex($"{jsRegexString}|{injectString}", RegexOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Creates a regex based on the provided strings.
        /// Treats these strings as simple, comma-delimited option lists,
        /// with possible * globs.
        /// </summary>
        /// <param name="simpleString">The user-defined filter string.</param>
        /// <param name="injectString">The default, program-provided string to filter on.</param>
        /// <returns>Returns a regex constructed from the strings.</returns>
        private Regex CreateSimpleRegex(string simpleString, string injectString)
        {
            if (string.IsNullOrEmpty(simpleString) && string.IsNullOrEmpty(injectString))
            {
                return EmptyRegex;
            }

            string safeGlobString = simpleString.RemoveUnsafeCharacters();

            var splits = safeGlobString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            string bar = "";

            // Convert comma-separated entries to |'d regex options.
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
            if (!string.IsNullOrEmpty(injectString))
                sb.Append($"{bar}{injectString}");

            try
            {
                return new Regex($@"{sb.ToString()}", RegexOptions.IgnoreCase);
            }
            catch (ArgumentException e)
            {
                Logger.Error($"Failed to create regex using string: [{sb.ToString()}]", e);
            }

            // If the attempt to create the regex to be returned failed, bail and
            // return a pure false regex.
            IsInverted = false;
            return alwaysFalse;
        }
        #endregion

        #region Public Use
        /// <summary>
        /// Function to test whether a provided string is matched by the current filter.
        /// </summary>
        /// <param name="input">The string to check against the filter.</param>
        /// <returns>Returns true (or false, if inverted) if the the input string is matched against the filter.</returns>
        public bool Match(string input) => filterRegex.Match(input).Success ^ IsInverted;

        /// <summary>
        /// Gets a value indicating whether this instance is uses the empty string regex.
        /// </summary>
        public bool IsEmpty => filterRegex == EmptyRegex;

        /// <summary>
        /// Gets a value indicating whether this instance is uses a null regex.
        /// A null regex will always return false on Match tests.
        /// </summary>
        public bool IsAlwaysFalse => filterRegex == alwaysFalse;

        /// <summary>
        /// Gets whether this instance inverts the results of a Match.
        /// </summary>
        public bool IsInverted { get; private set; } = false;
        #endregion
    }
}
