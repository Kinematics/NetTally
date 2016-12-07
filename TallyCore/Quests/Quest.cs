using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HtmlAgilityPack;
using NetTally.Adapters;
using NetTally.Utility;
using NetTally.Filters;

namespace NetTally
{
    /// <summary>
    /// The quest class is for storing a quest's thread name, and the starting and
    /// ending posts that are being used to construct a tally.
    /// </summary>
    public class Quest : IQuest
    {
        public Quest()
        {
            ThreadName = NewThreadEntry;
        }

        #region IPropertyChanged interface implementation
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Fields
        static readonly Regex displayNameRegex = new Regex(@"(?<displayName>[^/]+)(/|#[^/]*)?$");

        public const string NewThreadEntry = "https://forums.sufficientvelocity.com/threads/fake-thread.00000";

        public IForumAdapter ForumAdapter { get; private set; } = null;


        string threadName = string.Empty;
        string displayName = string.Empty;
        public string ThreadTitle { get; private set; } = string.Empty;

        int postsPerPage = 0;

        int startPost = 1;
        int endPost = 0;

        bool checkForLastThreadmark = true;
        bool useCustomThreadmarkFilters = false;
        string customThreadmarkFilters = string.Empty;

        bool useCustomTaskFilters = false;
        string customTaskFilters = string.Empty;

        PartitionMode partitionMode = PartitionMode.None;
        #endregion

        #region Initialization and Updating
        /// <summary>
        /// Gets the expected forum adapter for this quest.
        /// </summary>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public async Task InitForumAdapter() => await InitForumAdapter(CancellationToken.None).ConfigureAwait(false);

        /// <summary>
        /// Gets the expected forum adapter for this quest, with option to cancel.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public async Task InitForumAdapter(CancellationToken token)
        {
            if (ForumAdapter == null)
            {
                ForumAdapter = await ForumAdapterFactory.GetAdapter(this, token).ConfigureAwait(false);

                if (ForumAdapter == null)
                    throw new InvalidOperationException($"No forum adapter found for quest thread:\n{ThreadName}");

                if (postsPerPage == 0)
                    PostsPerPage = ForumAdapter.DefaultPostsPerPage;
            }
        }

        /// <summary>
        /// Call this if anything changes the thread name, as that means the forum adapter is now invalid.
        /// </summary>
        void UpdateForumAdapter()
        {
            if (ForumAdapter == null)
                return;

            try
            {
                ForumAdapter.Site = new Uri(ThreadName);
            }
            catch
            {
                ForumAdapter = null;
                PostsPerPage = 0;
            }
        }
        #endregion

        #region Descriptive/Tally Properties
        /// <summary>
        /// Gets the full thread URL for the quest.
        /// </summary>
        public string ThreadName
        {
            get
            {
                return threadName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Thread name cannot be null.");
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("URL cannot be empty.", nameof(value));


                threadName = CleanPageNumbers(value);
                UpdateThreadTitle();

                OnPropertyChanged();
                OnPropertyChanged("DisplayName");

                UpdateForumAdapter();
            }
        }

        /// <summary>
        /// Gets the name to display in the combo box dropdown, for selecting a quest.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayName))
                    return displayName;

                if (!string.IsNullOrEmpty(ThreadTitle))
                    return ThreadTitle;

                return threadName;
            }
            set
            {
                displayName = value.RemoveUnsafeCharacters();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enum for the type of partitioning to use when performing a tally.
        /// </summary>
        public PartitionMode PartitionMode
        {
            get { return partitionMode; }
            set
            {
                partitionMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Clean page anchors and page numbers from the provided thread URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string CleanPageNumbers(string url)
        {
            Regex pageNumberRegex = new Regex(@"^(?<base>.+?)(&?page[-=]?\d+)?(&p=?\d+)?(#[^/]*)?$");
            Match m = pageNumberRegex.Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return url.RemoveUnsafeCharacters();
        }

        /// <summary>
        /// Override ToString for class.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public override string ToString() => DisplayName;

        void UpdateThreadTitle()
        {
            Match m = displayNameRegex.Match(threadName);
            if (m.Success)
                ThreadTitle = m.Groups["displayName"].Value;
            else
                ThreadTitle = threadName;
        }

        #endregion

        #region Page Loading
        /// <summary>
        /// The number of posts per page for this forum thread.
        /// Auto-detect value if current field value is 0.
        /// </summary>
        public int PostsPerPage
        {
            get
            {
                if (postsPerPage == 0)
                {
                    var ppp = ForumAdapter?.DefaultPostsPerPage;
                    if (ppp.HasValue)
                    {
                        postsPerPage = ppp.Value;
                    }
                }
                return postsPerPage;
            }
            set
            {
                postsPerPage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of the post to start looking for votes in.
        /// Not valid below 1.
        /// </summary>
        public int StartPost
        {
            get { return startPost; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(StartPost), "Start post must be at least 1.");
                startPost = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of the last post to look for votes in.
        /// Not valid below 0.
        /// A value of 0 means it reads to the end of the thread.
        /// </summary>
        public int EndPost
        {
            get { return endPost; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(EndPost), "End post must be at least 0.");
                endPost = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        public bool CheckForLastThreadmark
        {
            get { return checkForLastThreadmark; }
            set
            {
                checkForLastThreadmark = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        public bool UseCustomThreadmarkFilters
        {
            get { return useCustomThreadmarkFilters; }
            set
            {
                useCustomThreadmarkFilters = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        public string CustomThreadmarkFilters
        {
            get { return customThreadmarkFilters; }
            set
            {
                customThreadmarkFilters = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to use custom threadmark filters to exclude threadmarks
        /// from the list of valid 'last threadmark found' checks.
        /// </summary>
        public bool UseCustomTaskFilters
        {
            get { return useCustomTaskFilters; }
            set
            {
                useCustomTaskFilters = value;
                TaskFilter = new TaskFilter(this);
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Custom threadmark filters to exclude threadmarks from the list of valid
        /// 'last threadmark found' checks.
        /// </summary>
        public string CustomTaskFilters
        {
            get { return customTaskFilters; }
            set
            {
                customTaskFilters = value;
                TaskFilter = new TaskFilter(this);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// This is updated any time UseCustomTaskFilters or CustomTaskFilters properties change.
        /// </summary>
        public TaskFilter TaskFilter { get; set; }

        /// <summary>
        /// Boolean value indicating if the tally system should read to the end
        /// of the thread.  This is done when the EndPost is 0.
        /// </summary>
        public bool ReadToEndOfThread => EndPost == 0 || ThreadmarkPost != 0;

        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        /// <param name="postNumber">The post number of the thread.</param>
        /// <returns>Returns the page number of the thread that the post should be on.</returns>
        public int GetPageNumberOf(int postNumber) => ((postNumber - 1) / PostsPerPage) + 1;

        /// <summary>
        /// Property to store any found threadmark post number.
        /// </summary>
        public int ThreadmarkPost { get; set; } = 0;

        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        /// <param name="pageProvider">The page provider that can be used to load web pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the number of the first page we should start loading.</returns>
        public async Task<ThreadRangeInfo> GetStartInfo(IPageProvider pageProvider, CancellationToken token)
        {
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            var startInfo = await ForumAdapter.GetStartingPostNumber(this, pageProvider, token);

            return startInfo;
        }

        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="threadRangeInfo">Info on which pages to load.</param>
        /// <param name="pageProvider">The page provider to use to load the pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>
        /// Returns a list of web pages as HTML Document tasks.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Throws an exception if it is
        /// unable to load even the first requested page.</exception>
        public async Task<List<Task<HtmlDocument>>> LoadQuestPages(ThreadRangeInfo threadRangeInfo, IPageProvider pageProvider, CancellationToken token)
        {
            // We will store the loaded pages in a new List.
            List<Task<HtmlDocument>> pages = new List<Task<HtmlDocument>>();

            int firstPageNumber = threadRangeInfo.GetStartPage(this);

            // Keep track of whether we used threadmarks to figure out the
            // first post.  If we did, we'll re-use this number when filtering
            // for valid posts.
            if (threadRangeInfo.IsThreadmarkSearchResult)
                if (threadRangeInfo.ID > 0)
                    ThreadmarkPost = threadRangeInfo.ID;
                else
                    ThreadmarkPost = -1;
            else
                ThreadmarkPost = 0;

            // Var for what we determine the last page number will be
            int lastPageNumber = 0;
            int pagesToScan = 0;

            if (threadRangeInfo.Pages > 0)
            {
                // If the startInfo obtained the thread pages info, just use that.
                lastPageNumber = threadRangeInfo.Pages;
                pagesToScan = lastPageNumber - firstPageNumber + 1;
            }
            else if (ReadToEndOfThread)
            {
                // If we're reading to the end of the thread (end post 0, or based on a threadmark),
                // then we need to load the first page to find out how many pages there are in the thread.
                // Make sure to bypass the cache, since it may have changed since the last load.

                HtmlDocument firstPage = await pageProvider.GetPage(ForumAdapter.GetUrlForPage(firstPageNumber, PostsPerPage),
                    $"Page {firstPageNumber}", CachingMode.BypassCache, token, true).ConfigureAwait(false);

                if (firstPage == null)
                    throw new InvalidOperationException($"Unable to load web page: {ForumAdapter.GetUrlForPage(firstPageNumber, PostsPerPage)}");

                pages.Add(Task.FromResult(firstPage));

                ThreadInfo threadInfo = ForumAdapter.GetThreadInfo(firstPage);
                lastPageNumber = threadInfo.Pages;

                // Get the number of pages remaining to load
                pagesToScan = lastPageNumber - firstPageNumber;
                // Increment the first page number to fix where we're starting.
                firstPageNumber++;
            }
            else
            {
                // If we're not reading to the end of the thread, just calculate
                // what the last page number will be.  Pages to scan will be the
                // difference in pages +1.
                lastPageNumber = GetPageNumberOf(EndPost);
                pagesToScan = lastPageNumber - firstPageNumber + 1;
            }

            // Initiate the async tasks to load the pages
            if (pagesToScan > 0)
            {
                // Initiate tasks for all pages other than the first page (which we already loaded)
                var results = from pageNum in Enumerable.Range(firstPageNumber, pagesToScan)
                                let pageUrl = ForumAdapter.GetUrlForPage(pageNum, PostsPerPage)
                                let shouldCache = (pageNum != lastPageNumber)
                                select pageProvider.GetPage(pageUrl, $"Page {pageNum}", CachingMode.UseCache, token, shouldCache);

                pages.AddRange(results.ToList());
            }

            return pages;
        }
        #endregion

        #region IComparable implementation
        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that
        /// indicates whether the current instance precedes, follows, or occurs in the same position in the
        /// sort order as the other object.
        /// Sort order is determined by DisplayName, with a case-insensitive check.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// Returns -1 if this is before obj, 0 if it's the same, and 1 if it's after obj.
        /// </returns>
        public int CompareTo(object obj) => Compare(this, obj as IQuest);

        public int CompareTo(IQuest other) => Compare(this, other);

        public override bool Equals(object obj)
        {
            IQuest other = obj as IQuest;
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (string.Compare(ThreadName.ToLowerInvariant(), other.ThreadName.ToLowerInvariant(), StringComparison.Ordinal) != 0)
                return false;

            return string.Compare(DisplayName.ToLowerInvariant(), other.DisplayName.ToLowerInvariant(), StringComparison.Ordinal) == 0;
        }

        // Note: Do not implement GetHashCode based on mutable properties.  It will break combobox binding.
        //public override int GetHashCode() => DisplayName.GetHashCode();

        /// <summary>
        /// IComparer function.
        /// </summary>
        /// <param name="left">The first object being compared.</param>
        /// <param name="right">The second object being compared.</param>
        /// <returns>Returns a negative value if left is 'before' right, 0 if they're equal, and
        /// a positive value if left is 'after' right.</returns>
        public static int Compare(IQuest left, IQuest right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (ReferenceEquals(left, null))
                return -1;
            if (ReferenceEquals(right, null))
                return 1;

            return string.Compare(left.DisplayName.ToLowerInvariant(), right.DisplayName.ToLowerInvariant(), StringComparison.Ordinal);
        }

        public static bool operator ==(Quest left, Quest right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }

        public static bool operator !=(Quest left, Quest right) => !(left == right);

        public static bool operator <(Quest left, Quest right) => (Compare(left, right) < 0);

        public static bool operator >(Quest left, Quest right) => (Compare(left, right) > 0);

        #endregion
    }
}