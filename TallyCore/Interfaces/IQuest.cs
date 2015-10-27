using System;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

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
        int RawPostsPerPage { get; set; }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
        /// <summary>
        /// Flag for whether to use vote partitions for this quest.
        /// OBSOLETE
        /// </summary>
        [Obsolete("Use PartitionMode instead")]
        bool UseVotePartitions { get; set; }
        /// <summary>
        /// Flag for whether vote partitions should be done by line or by block.
        /// OBSOLETE
        /// </summary>
        [Obsolete("Use PartitionMode instead")]
        bool PartitionByLine { get; set; }
        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        PartitionMode PartitionMode { get; set; }

        /// <summary>
        /// Flag for whether to count votes using preferential vote ranking.
        /// </summary>
        bool AllowRankedVotes { get; set; }

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

        /// <summary>
        /// Get the forum adapter needed to read results from the web site this
        /// quest is for.  Allow it to run async so that it can load web pages.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IForumAdapter> GetForumAdapterAsync(CancellationToken token);


        /// <summary>
        /// Get the URL string for the provided page number of the quest thread.
        /// </summary>
        string GetPageUrl(int pageNumber);
        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        int GetPageNumberOf(int postNumber);
        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        Task<int> GetFirstPageNumber(IPageProvider pageProvider, CancellationToken token);
        /// <summary>
        /// Get the last page number of the thread, where we should stop reading, based on
        /// current quest parameters and the provided web page.
        /// </summary>
        Task<int> GetLastPageNumber(HtmlDocument loadedPage, CancellationToken token);
    }
}
