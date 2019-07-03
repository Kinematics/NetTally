using System;

namespace NetTally.Utility
{
    /// <summary>
    /// A static class to allow setting a 'default' Uri where needed.
    /// </summary>
    public static class DefaultUri
    {
        /// <summary>
        /// The static default Uri to be used in the program.
        /// </summary>
        public static Uri Default = new Uri(@"http://example.com/");
        public static Uri DefaultSV = new Uri(@"https://example.com/threads/example.101/");
    }
}
