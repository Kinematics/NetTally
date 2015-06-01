using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally
{
    public static class Utility
    {
        // Regex for control and formatting characters that we don't want to allow processing of
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"\p{Cc}|\p{Cf}");

        public static string SafeString(string input)
        {
            return UnsafeCharsRegex.Replace(input, "");
        }
    }
}
