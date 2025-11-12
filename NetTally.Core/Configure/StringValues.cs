namespace NetTally.Configure;
/// <summary>
/// Class for general static functions relating to text manipulation and comparisons.
/// </summary>
public static partial class Strings
{
    public const string PlanNameMarker = "◈";
    public const char PlanNameMarkerChar = '◈';
    public const string NoRankMarker = "⊘";
    public const string NonVotingMarker = "-";
    public const string UnknownMarker = "?";
    public const string VoteMarker = "X";
    public const string ApprovalMarker = "±";
    public const string ScoreMarker = "%";
    public const string RankMarker = "#";

    public const char OpenBBCode = '『';
    public const char CloseBBCode = '』';
    public const char OpenStrike = '❰';
    public const char CloseStrike = '❱';
    public const char StrikeNewLine = '⦂';

    public const string OmakeFilter = @"\bomake\b";
    public const string NewThreadEntry = "https://www.example.com/threads/fake-thread.00000";
    public const string NewThreadDisplayName = "~Placeholder~";

    public const string ExampleHostUrl = "http://www.example.com/";
    public static readonly Uri ExampleUri = new(ExampleHostUrl);

    public const string UntitledThread = "~Untitled~";
    public const string UnknownAuthor = "⟦Unknown⟧";
    public const string NoAuthor = "⟦None⟧";
    public const string NoTask = "【NONE】";
    public const string NoTask1 = "⟦NONE⟧";
}
