using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally
{
    public static class DebugMode
    {
        static readonly Regex debugRegex = new Regex(@"\bdebug\b");

        public static bool Active { get; private set; }

        public static void Update()
        {
            Active = CheckForDebugFile();
        }

        /// <summary>
        /// If the user creates a file with the word "debug" in the filename, activate
        /// debugmode for the program.
        /// </summary>
        /// <returns>Returns true if an appropriate file is found.</returns>
        private static bool CheckForDebugFile()
        {
            var files = Directory.EnumerateFiles(".");

            return files.Any(f => debugRegex.Match(f).Success);
        }
    }
}
