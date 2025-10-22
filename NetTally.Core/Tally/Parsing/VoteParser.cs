using System.Text.RegularExpressions;
using NetTally.Models.Creation;
using NetTally.Models.Behavior;
using NetTally.Models.Votes;
using NetTally.Utility;
using NetTally.Models.Defaults;

namespace NetTally.Tally.Parsing;

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
        return TallyPostRegex.IsMatch(cleanText);
    }

    private static List<VoteLine> GetVoteLines(List<string> textLines)
    {
        var voteLines = textLines
            .Select(t => VoteLineParser.ParseLineParts(t))
            .Where(a => a != VoteLine.None)
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

    private static List<VoteLine> GetNominationLines(List<string> textLines)
    {
        var voteLines = textLines
            .Select(t => NominationLineRegex.Match(t))
            .Where(m => m.Success)
            .Select(m => VoteLine.Create(Prefix.None,
                                Marker.Create("X"),
                                VoteTask.None,
                                VoteContent.Create(m.Groups["username"].Value)))
            .Where(v => v != VoteLine.None)
            .ToList();

        if (voteLines.Count == textLines.Count)
            return voteLines;

        return [];
    }
    #endregion Support Methods
}
