using System.Text.RegularExpressions;

namespace NetTally.Adapters
{
    public static class ThreadmarkFilter
    {
        static readonly Regex omakeRegex = new Regex(@"\bomake\b", RegexOptions.IgnoreCase);

        public static bool Filter(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            return omakeRegex.Match(title).Success;
        }
    }
}
