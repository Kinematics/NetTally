using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering;

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
public partial class RegexPattern
{
    private Regex Regex { get; }
    private bool Invert { get; }

    private RegexPattern(Regex regex, bool invert)
    {
        Regex = regex;
        Invert = invert;
    }

    #region Factory
    public static RegexPattern Create(Regex regex, bool invert = false)
    {
        ArgumentNullException.ThrowIfNull(regex);
        return new RegexPattern(regex, invert);
    }

    public static RegexPattern Create(string pattern)
    {
        var (regex, inverted) = CreateRegexFrom(pattern);
        return new RegexPattern(regex, inverted);
    }
    #endregion Factory

    #region Construction of a regex pattern
    /// <summary>
    /// A regex that contains nothing.
    /// </summary>
    [GeneratedRegex("^$")]
    private static partial Regex EmptyRegex();

    [GeneratedRegex(@"(\w)\]", RegexOptions.None, 100)]
    private static partial Regex PostWordRegex();

    [GeneratedRegex(@"\[(\w)", RegexOptions.None, 100)]
    private static partial Regex PreWordRegex();

    [GeneratedRegex(@"[*]", RegexOptions.None, 100)]
    private static partial Regex SplatRegex();

    [GeneratedRegex(@"[?]", RegexOptions.None, 100)]
    private static partial Regex LetterRegex();

    [GeneratedRegex(@"([.(){}^$])", RegexOptions.None, 100)]
    private static partial Regex EscapeCharsRegex();

    [GeneratedRegex(@"^/(?<regex>.+)/(?<options>[ugi]{0,3})$",
        RegexOptions.None, 100)]
    private static partial Regex JSRegex();

    /// <summary>
    /// A pure false regex, in as simple a form as possible.  From the start of the line,
    /// require a negative lookahead for a value that is followed by that value.
    /// </summary>
    [GeneratedRegex(@"^(?!x)x")]
    private static partial Regex AlwaysFalseRegex();


    static readonly Regex emptyRegex = EmptyRegex();
    static readonly Regex postWord = PostWordRegex();
    static readonly Regex preWord = PreWordRegex();
    static readonly Regex splat = SplatRegex();
    static readonly Regex letter = LetterRegex();
    static readonly Regex escapeChars = EscapeCharsRegex();
    static readonly Regex jsRegex = JSRegex();
    static readonly Regex falseRegex = AlwaysFalseRegex();
    static readonly char[] separator = [','];


    private static (Regex regex, bool inverted) CreateRegexFrom(string pattern)
    {
        pattern = pattern.RemoveUnsafeCharacters().Trim();

        bool invert = CheckForInversion(ref pattern);

        return (CheckIfJs(ref pattern) 
                ? CreateJsRegex(pattern)
                : CreateSimpleRegex(pattern),
                invert);
    }

    private static bool CheckForInversion(ref string pattern)
    {
        bool invert = false;

        if (pattern.Length > 0)
        {
            invert = pattern[0] == '!';
            if (invert)
                pattern = pattern[1..].TrimStart();
        }

        return invert;
    }

    private static bool CheckIfJs(ref string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return false;

        Match m = jsRegex.Match(pattern);
        if (m.Success)
        {
            pattern = m.Groups["regex"].Value;
        }

        return m.Success;
    }

    private static Regex CreateJsRegex(string pattern)
    {
        return new Regex(pattern,
            RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
    }

    private static Regex CreateSimpleRegex(string pattern)
    {
        string correctedPattern = pattern
                                  .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                  .Select(p => escapeChars.Replace(p, "\\$1"))
                                  .Select(p => letter.Replace(p, @"."))
                                  .Select(p => splat.Replace(p, @".*?"))
                                  .Select(p => preWord.Replace(p, "\\b$1"))
                                  .Select(p => postWord.Replace(p, "$1\\b"))
                                  .DefaultIfEmpty("")
                                  .Aggregate((a, b) => $"{a}|{b}");

        Regex regex = new(correctedPattern,
            RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture,
            TimeSpan.FromMilliseconds(100));

        return regex;
    }
    #endregion Construction of a regex pattern

    #region Public Methods
    /// <summary>
    /// Tests whether a provided item string matches the regex pattern.
    /// </summary>
    /// <param name="item">The string to check.</param>
    /// <returns>Returns true if the item matches, or false if not.</returns>
    public bool IsMatch(string item) => Regex.IsMatch(item) ^ Invert;

    public bool IsInverted => Invert;
    #endregion Public Methods
}
