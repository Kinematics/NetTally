using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;
using NetTally.Filters;

namespace NetTally
{
    public interface IQuest : INotifyPropertyChanged, IComparable
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
        /// The URI that represents the thread URL string.
        /// </summary>
        Uri ThreadUri { get; }
        /// <summary>
        /// The type of forum used at the URI site.
        /// </summary>
        ForumType ForumType { get; set; }
        /// <summary>
        /// Get the forum adapter being used by this quest.
        /// Must be initialized first.
        /// </summary>
        IForumAdapter ForumAdapter { get; set; }

        /// <summary>
        /// The number of posts per page for this forum thread.
        /// </summary>
        int PostsPerPage { get; set; }
        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        int GetPageNumberOf(int postNumber);

        /// <summary>
        /// Starting post to start tallying from.
        /// </summary>
        int StartPost { get; set; }
        /// <summary>
        /// Ending post for the tally to run to.
        /// </summary>
        int EndPost { get; set; }
        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        bool UseCustomThreadmarkFilters { get; set; }
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        string CustomThreadmarkFilters { get; set; }
        /// <summary>
        /// Gets or sets the threadmark filter, based on current threadmark filter settings.
        /// </summary>
        Filter ThreadmarkFilter { get; }

        /// <summary>
        /// Flag for whether to use custom filters to only process specified tasks.
        /// </summary>
        bool UseCustomTaskFilters { get; set; }
        /// <summary>
        /// List of custom tasks to process.
        /// </summary>
        string CustomTaskFilters { get; set; }
        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// </summary>
        Filter TaskFilter { get; }

        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        PartitionMode PartitionMode { get; set; }

        /// <summary>
        /// Derived property.
        /// </summary>
        bool ReadToEndOfThread { get; }


        // Obsolete:



        /// <summary>
        /// Store the found threadmark post number.
        /// </summary>
        int ThreadmarkPost { get; set; }

        /// <summary>
        /// The quest title as derived from the thread name.
        /// </summary>
        string ThreadTitle { get; }

        /// <summary>
        /// Initialize the forum adapter needed to read results from the web site this quest is for.
        /// Runs async, but doesn't need a cancellation token.
        /// </summary>
        /// <returns>Returns nothing.</returns>
        Task InitForumAdapterAsync();

        /// <summary>
        /// Initialize the forum adapter needed to read results from the web site this quest is for.
        /// Runs async, with a cancellation token.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns nothing.</returns>
        Task InitForumAdapterAsync(CancellationToken token);

        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        Task<ThreadRangeInfo> GetStartInfoAsync(IPageProvider pageProvider, CancellationToken token);

        /// <summary>
        /// Asynchronously load pages for the specified quest.
        /// </summary>
        /// <param name="pageProvider">The page provider to use to load the pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a list of HTML documents defined by the requested quest.</returns>
        Task<List<Task<HtmlDocument>>> LoadQuestPagesAsync(ThreadRangeInfo threadRangeInfo, IPageProvider pageProvider, CancellationToken token);
    }
}
