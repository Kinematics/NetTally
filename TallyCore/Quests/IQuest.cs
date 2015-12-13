using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;

namespace NetTally
{
    public interface IQuest : INotifyPropertyChanged
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
        /// The quest title as derived from the thread name.
        /// </summary>
        string ThreadTitle { get; }


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
        /// Initialize the forum adapter needed to read results from the web site this quest is for.
        /// Runs async, but doesn't need a cancellation token.
        /// </summary>
        /// <returns>Returns nothing.</returns>
        Task InitForumAdapter();

        /// <summary>
        /// Initialize the forum adapter needed to read results from the web site this quest is for.
        /// Runs async, with a cancellation token.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns nothing.</returns>
        Task InitForumAdapter(CancellationToken token);

        /// <summary>
        /// Get the forum adapter being used by this quest.
        /// Must be initialized first.
        /// </summary>
        IForumAdapter ForumAdapter { get; }

        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        int GetPageNumberOf(int postNumber);

        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        Task<ThreadRangeInfo> GetStartInfo(IPageProvider pageProvider, CancellationToken token);

        /// <summary>
        /// Asynchronously load pages for the specified quest.
        /// </summary>
        /// <param name="pageProvider">The page provider to use to load the pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a list of HTML documents defined by the requested quest.</returns>
        Task<List<Task<HtmlDocument>>> LoadQuestPages(ThreadRangeInfo threadRangeInfo, IPageProvider pageProvider, CancellationToken token);
    }
}
