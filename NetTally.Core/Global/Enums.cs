using NetTally.Attributes;

namespace NetTally
{
    /// <summary>
    /// Enums used in the Web namespace
    /// </summary>
    namespace Web
    {
        /// <summary>
        /// Potential status types for page loads.
        /// </summary>
        public enum PageRequestStatusType
        {
            None,
            Requested,
            LoadedFromCache,
            Loaded,
            Retry,
            Error,
            Failed,
            Cancelled,
        }

        /// <summary>
        /// Enum for whether to use the cache when loading a web page.
        /// </summary>
        public enum CachingMode
        {
            UseCache,
            BypassCache
        }

        /// <summary>
        /// Flag whether a requested page should be cached.
        /// </summary>
        public enum ShouldCache
        {
            Yes,
            No
        }

        /// <summary>
        /// Flag whether to suppress notifications when loading pages.
        /// </summary>
        public enum SuppressNotifications
        {
            No,
            Yes
        }

        /// <summary>
        /// The type of page being loaded.
        /// </summary>
        public enum PageType
        {
            Thread,
            Threadmarks,
        }
    }

    /// <summary>
    /// Enums used by the Forums namespace
    /// </summary>
    namespace Forums
    {
        /// <summary>
        /// The type of forum being read.
        /// </summary>
        public enum ForumType
        {
            Unknown,
            XenForo,
            vBulletin3,
            vBulletin4,
            vBulletin5,
            phpBB,
            NodeBB
        }
    }

    /// <summary>
    /// Enums used in the Votes namespace
    /// </summary>
    namespace Votes
    {
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

        public enum MarkerType
        {
            None,
            Vote,
            Rank,
            Score,
            Approval,
            Continuation
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
            ByLineTask, // obsolete this; partition by line should always carry in parent tasks
            [EnumDescription("Partition By Block")]
            ByBlock, // should automatically partition label plans
            [EnumDescription("Partition (Plans) By Block")]
            ByBlockAll, // only used to partition content plans
        }

        /// <summary>
        /// The proxy reference type of a vote line.
        /// </summary>
        public enum ReferenceType
        {
            Label,
            Any,
            Voter,
            Plan,
        }

        public enum PlanType
        {
            SingleLine,
            Label,
            Content,
            Base
        }

        public enum IdentityType
        {
            User,
            Plan
        }
    }

    /// <summary>
    /// Enums used in the VoteCounting namespace
    /// </summary>
    namespace VoteCounting
    {
        public enum StandardVoteCounterMethod
        {
            Default
        }

        public enum ApprovalVoteCounterMethod
        {
            Default
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
            [EnumDescription("Format for Democratix")]
            Democratix,
            [EnumDescription("Format for condorcet.vote")]
            condorcetvote,
        }

        /// <summary>
        /// Enum for type of ordering to use when sorting vote tasks.
        /// </summary>
        public enum TasksOrdering
        {
            AsTallied,
            Alphabetical,
        }
    }

    /// <summary>
    /// Enums used in the Output namespace
    /// </summary>
    namespace Output
    {
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
    }
}
