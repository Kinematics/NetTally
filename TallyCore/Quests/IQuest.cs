using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;

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
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
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
        Task InitForumAdapter();

        /// <summary>
        /// Get the forum adapter needed to read results from the web site this
        /// quest is for.  Allow it to run async so that it can load web pages.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task InitForumAdapter(CancellationToken token);

        IForumAdapter ForumAdapter { get; }

        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        int GetPageNumberOf(int postNumber);
        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        Task<ThreadStartValue> GetStartInfo(IPageProvider pageProvider, CancellationToken token);

        /// <summary>
        /// Asynchronously load pages for the specified quest.
        /// </summary>
        /// <param name="pageProvider">The page provider to use to load the pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a list of HTML documents defined by the requested quest.</returns>
        Task<List<HtmlDocument>> LoadQuestPages(IPageProvider pageProvider, CancellationToken token);
    }
}
