using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally
{
    public static class DebugMode
    {
        public static bool Active => CheckForDebugFile();
        static readonly Regex debugRegex = new Regex(@"\bdebug\b");

        /// <summary>
        /// If the user creates a file with the word "debug" in the filename, activate
        /// debugmode for the program.
        /// </summary>
        /// <returns></returns>
        private static bool CheckForDebugFile()
        {
            var files = Directory.EnumerateFiles(".");

            return files.Any(f => debugRegex.Match(f).Success);
        }
    }
}
