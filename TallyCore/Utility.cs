using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally
{
    // Local enum for separating vote categories
    public enum VoteType
    {
        Vote,
        Plan,
        Rank
    }

    public static class Utility
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        public static string SafeString(string input)
        {
            return UnsafeCharsRegex.Replace(input, "");
        }
    }
}
