using NetTally.Attributes;

namespace NetTally.Types.Enums
{
    /// <summary>
    /// Enum for determining which type of rank vote calculation method to use.
    /// </summary>
    public enum RankVoteCounterMethod
    {
        [EnumDescription("Default (RIR)")]
        Default,
        [EnumDescription("Wilson Scoring")]
        Wilson,
        [EnumDescription("Schulze (Condorcet)")]
        Schulze,
        [EnumDescription("Baldwin Runoff")]
        Baldwin,
        [EnumDescription("Rated Instant Runoff")]
        RIRV,
    }
}
