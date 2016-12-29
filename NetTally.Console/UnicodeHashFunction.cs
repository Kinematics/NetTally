using System;
using System.Globalization;

namespace NetTally.Platform
{
    public static class UnicodeHashFunction
    {
        /// <summary>
        /// Hashes the provided string using the given CompareOptions.
        /// Doing this allows all custom compare options to be applied in determining the hash value.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="info">The CompareInfo object doing the unicode-aware comparison.</param>
        /// <param name="options">The options to apply to the comparison.</param>
        /// <returns>Returns the hash code for the string.</returns>
        public static int HashFunction(string str, CompareInfo info, CompareOptions options)
        {
            return 0;
        }
    }
}
