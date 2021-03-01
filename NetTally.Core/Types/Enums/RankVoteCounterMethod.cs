using System.ComponentModel;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for determining which type of rank vote calculation method to use.
    /// </summary>
    public enum RankVoteCounterMethod
    {
        [Description("Default (RIR)")]
        Default,
        [Description("Wilson Scoring")]
        Wilson,
        [Description("Schulze (Condorcet)")]
        Schulze,
        [Description("Baldwin Runoff")]
        Baldwin,
        [Description("Rated Instant Runoff")]
        RIRV,
    }
}
