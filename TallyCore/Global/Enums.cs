using System;

namespace NetTally
{
    // File containing all defined enums for the program.
        
    /// <summary>
    /// Enum for separating vote categories
    /// </summary>
    public enum VoteType
    {
        Vote,
        Plan,
        Rank,
        Approval
    }

    /// <summary>
    /// Enum for whether to use the cache when loading a web page.
    /// </summary>
    public enum CachingMode
    {
        UseCache,
        BypassCache
    }

    public enum StandardVoteCounterMethod
    {
        Default
    }

    public enum ApprovalVoteCounterMethod
    {
        Default
    }

    public enum ReferenceType
    {
        Label,
        Any,
        Voter,
        Plan,
    }

    public enum PageType
    {
        Thread,
        Threadmarks,
    }

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

    /// <summary>
    /// Enum for various modes of displaying the tally results.
    /// </summary>
    public enum DisplayMode
    {
        [EnumDescription("Normal")]
        Normal,
        [EnumDescription("Spoiler Voters")]
        SpoilerVoters,
        [EnumDescription("Spoiler All")]
        SpoilerAll,
        [EnumDescription("Normal, No Voters")]
        NormalNoVoters,
        [EnumDescription("Compact")]
        Compact,
        [EnumDescription("Compact, No Voters")]
        CompactNoVoters
    }

    /// <summary>
    /// Enum for various partitioning modes to use for breaking votes up into components.
    /// </summary>
    public enum PartitionMode
    {
        [EnumDescription("No Partitioning")]
        None,
        [EnumDescription("Partition By Line")]
        ByLine,
        [EnumDescription("Partition By Line (+Task)")]
        ByLineTask,
        [EnumDescription("Partition By Block")]
        ByBlock,
        [EnumDescription("Partition (Plans) By Block")]
        ByBlockAll,
    }
}
