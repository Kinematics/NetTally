using System.Threading;
using System.Threading.Tasks;

namespace NetTally
{
    public interface IQuest
    {
        /// <summary>
        /// The name of the thread to be queried.
        /// </summary>
        string ThreadName { get; set; }
        /// <summary>
        /// The display name that the user can select.
        /// </summary>
        string DisplayName { get; set; }
        /// <summary>
        /// The base site name that can be used to get the forum adapter.
        /// </summary>
        string SiteName { get; }


        /// <summary>
        /// The web site that the quest is on.
        /// OBSOLETE
        /// </summary>
        string Site { get; set; }
        /// <summary>
        /// The name of the quest (ie: the name of the thread in the URL).
        /// OBSOLETE
        /// </summary>
        string Name { get; set; }


        /// <summary>
        /// Starting post to start tallying from.
        /// </summary>
        int StartPost { get; set; }
        /// <summary>
        /// Ending post for the tally to run to.
        /// </summary>
        int EndPost { get; set; }

        /// <summary>
        /// The number of posts per page for this forum thread.
        /// </summary>
        int PostsPerPage { get; set; }
        /// <summary>
        /// Get the number of posts per page for this forum thread.
        /// Raw value, without attempt at auto-fill.
        /// </summary>
        int RawPostsPerPage { get; }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
        /// <summary>
        /// Flag for whether to use vote partitions for this quest.
        /// </summary>
        bool UseVotePartitions { get; set; }
        /// <summary>
        /// Flag for whether vote partitions should be done by line or by block.
        /// </summary>
        bool PartitionByLine { get; set; }

        /// <summary>
        /// Derived property.
        /// </summary>
        bool ReadToEndOfThread { get; }

        /// <summary>
        /// Store the found threadmark post number.
        /// </summary>
        int ThreadmarkPost { get; set; }

        /// <summary>
        /// Return either the StartPost or the ThreadmarkPost, depending on config.
        /// </summary>
        int FirstTallyPost { get; }

        /// <summary>
        /// Get the forum adapter needed to read results from the web site this
        /// quest is for.
        /// </summary>
        /// <returns></returns>
        IForumAdapter GetForumAdapter();
        Task<IForumAdapter> GetForumAdapterAsync(CancellationToken token);
    }
}
