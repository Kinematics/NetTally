using System.Text.RegularExpressions;
using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;
using NetTally.Utility;

namespace NetTally.Tally.Components.Votes;
public static partial class VoteParser
{
    #region Regex
    // A post with ##### at the start of one of the lines is a posting of tally results.
    [GeneratedRegex(@"^#####", RegexOptions.Multiline)]
    private static partial Regex TallyPostRegex { get; }

    // A line solely composed of a callout to a given user is used for nomination tallying.
    [GeneratedRegex(@"^『url=""[^""]+?/members/\d+/""』@?(?<username>[^『]+)『/url』\s*$")]
    private static partial Regex NominationLineRegex { get; }
    #endregion Regex

    #region Public Methods
    public static List<VoteLine> ExtractVoteLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || IsTallyPost(text))
            return [];

        var textLines = text.GetStringLines();

        var voteLines = GetVoteLines(textLines);

        if (voteLines.Count == 0)
            voteLines = GetNominationLines(textLines);

        return voteLines;
    }
    #endregion Public Methods

    #region Support Methods
    private static bool IsTallyPost(string text)
    {
        string cleanText = VoteLineParser.StripBBCode(text);
        return TallyPostRegex.Match(cleanText).Success;
    }

    private static List<VoteLine> GetVoteLines(List<string> textLines)
    {
        var voteLines = textLines
            .Select(ParseLine)
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();

        if (voteLines.Count > 0)
        {
            if (voteLines[0].Prefix.Depth > 0)
            {
                voteLines[0] = voteLines[0].FullPromote();
            }
        }

        return voteLines;
    }

    private static VoteLine? ParseLine(string t)
    {
        var parsed = VoteLineParser.ParseLineParts(t);

        if (parsed == null) return null;

        var parts = parsed.Value;

        var prefix = Prefix.Create(parts.Prefix);
        var marker = Marker.Create(parts.Marker);
        var task = VoteTask.Create(parts.Task);
        var content = VoteContent.Create(parts.Content);

        var voteLine = VoteLine.Create(prefix, marker, task, content);

        return voteLine;
    }

    private static List<VoteLine> GetNominationLines(List<string> textLines)
    {
        var voteLines = textLines
            .Select(t => NominationLineRegex.Match(t))
            .Where(m => m.Success)
            .Select(m => VoteLine.Create(Prefix.Empty,
                                Marker.Create("X"),
                                VoteTask.Empty,
                                VoteContent.Create(m.Groups["username"].Value)))
            .Where(v => v != VoteLine.Empty)
            .ToList();

        if (voteLines.Count == textLines.Count)
            return voteLines;

        return [];
    }
    #endregion Support Methods
}
