using System;
using System.ComponentModel;
using NetTally.Forums;
using NetTally.Utility;
using NetTally.Votes;

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
        /// Derived property.  Is true if EndPost is 0.
        /// </summary>
        bool ReadToEndOfThread { get; }

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
        /// Flag for whether to use custom filters to exclude specified users from the tally.
        /// </summary>
        bool UseCustomUsernameFilters { get; set; }
        /// <summary>
        /// List of custom users to filter.
        /// </summary>
        string CustomUsernameFilters { get; set; }
        /// <summary>
        /// Gets or sets the user filter, based on current user filter settings.
        /// </summary>
        Filter UsernameFilter { get; }

        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        PartitionMode PartitionMode { get; set; }

    }
}
