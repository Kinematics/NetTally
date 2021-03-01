using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering
{
    /// <summary>
    /// A class that takes a string pattern and forms a regex object
    /// that corresponds to it.
    /// It may use a javascript-like format to provide a literal regex pattern,
    /// or it may provide a comma-delimited list.
    /// It may also invert the validity of the match by starting the pattern string
    /// with an exclamation mark.
    /// 
    /// Alice, Bob, Charlie
    /// !Alice, Bob, Charlie
    /// /something|other/
    /// !/something|other/
    /// </summary>
    public class RegexPattern
    {
        private Regex Regex { get; }
        private bool Invert { get; }

        public RegexPattern(Regex regex, bool invert = false)
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            Invert = invert;
        }

        public RegexPattern(string pattern)
        {
            pattern ??= string.Empty;
            (Regex, Invert) = CreateRegexFromPattern(pattern);
        }

        public bool IsMatch(string item)
        {
            if (item is null)
                return false;

            return Regex.IsMatch(item) ^ Invert;
        }

        static readonly Regex jsRegex = new Regex(@"^(?<invert>!)?/(?<regex>.+)/(?<options>[ugi]{0,3})$",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(150));
        static readonly Regex escapeChars = new Regex(@"([.?(){}^$\[\]])",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));
        static readonly Regex splat = new Regex(@"\*",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));
        static readonly Regex preWord = new Regex(@"^\w",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));
        static readonly Regex postWord = new Regex(@"\w$",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));

        private (Regex, bool) CreateRegexFromPattern(string pattern)
        {
            pattern = pattern.RemoveUnsafeCharacters().Trim();

            Regex regex;
            bool invert = false;

            // Check for javascript formatting first.
            Match m = jsRegex.Match(pattern);
            if (m.Success)
            {
                invert = m.Groups["invert"].Success;

                regex = new Regex(m.Groups["regex"].Value,
                    RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(100));
            }
            else
            {
                // Otherwise check generic comma-delimited formatting.

                if (pattern.StartsWith('!'))
                {
                    invert = true;
                    pattern = pattern[1..];
                }

                string[] patterns = pattern.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                string correctedPatterns = patterns
                                          .Select(p => p.Trim())
                                          .Select(p => escapeChars.Replace(p, "\\$1"))
                                          .Select(p => splat.Replace(p, @".*?"))
                                          .Select(p => preWord.IsMatch(p) ? @$"\b{p}" : p)
                                          .Select(p => postWord.IsMatch(p) ? @$"{p}\b" : p)
                                          .DefaultIfEmpty("")
                                          .Aggregate((a, b) => $"{a}|{b}");

                regex = new Regex(correctedPatterns,
                    RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
                    TimeSpan.FromMilliseconds(100));
            }

            return (regex, invert);
        }
    }
}
