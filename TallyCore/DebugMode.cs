using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        private bool CheckForDebugFile()
        {
            Regex r = new Regex(@"\bdebug\b");
            var files = Directory.EnumerateFiles(".");

            return files.Any(f => r.Match(f).Success);
        }

    }
}
