using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally
{
    public sealed class DebugMode
    {
        // Prevent outside instantiation
        private DebugMode()
        {
            Active = CheckForDebugFile();
        }

        static DebugMode()
        {
        }

        private static readonly DebugMode _singleton = new DebugMode();

        public static DebugMode Instance { get { return _singleton; } }

        public bool Active { get; }

        /// <summary>
        /// If the user creates a file with the word "debug" in the filename, activate
        /// debugmode for the program.
        /// </summary>
        /// <returns></returns>
        private bool CheckForDebugFile()
        {
            Regex r = new Regex(@"\bdebug\b");
            var files = Directory.EnumerateFiles(".");

            return files.Any(f => r.Match(f).Success);
        }
    }
}
