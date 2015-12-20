using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;
using NetTally.Utility;

namespace NetTally
{
    /// <summary>
    /// The quest class is for storing a quest's thread name, and the starting and
    /// ending posts that are being used to construct a tally.
    /// </summary>
    [DataContract(Name ="Quest")]
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
        static readonly Regex siteRegex = new Regex(@"^(?<siteName>https?://[^/]+/)");
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
                displayName = StringUtility.SafeString(value);
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
        public string CleanPageNumbers(string url)
        {
            Regex pageNumberRegex = new Regex(@"^(?<base>.+?)(&?page[-=]?\d+)?(&p=?\d+)?(#[^/]*)?$");
            Match m = pageNumberRegex.Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return StringUtility.SafeString(url);
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
        /// <param name="quest">Quest object containing query parameters.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<Task<HtmlDocument>>> LoadQuestPages(ThreadRangeInfo threadRangeInfo, IPageProvider pageProvider, CancellationToken token)
        {
            // We will store the loaded pages in a new List.
            List<Task<HtmlDocument>> pages = new List<Task<HtmlDocument>>();

            int firstPageNumber = threadRangeInfo.GetStartPage(this);

            // Keep track of whether we used threadmarks to figure out the
            // first post.  If we did, we'll re-use this number when filtering
            // for valid posts.
            ThreadmarkPost = threadRangeInfo.ID;

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
                    $"Page {firstPageNumber}", Caching.BypassCache, token).ConfigureAwait(false);

                if (firstPage == null)
                    throw new InvalidOperationException("Unable to load web page.");

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
                                select pageProvider.GetPage(pageUrl, $"Page {pageNum}", Caching.UseCache, token, shouldCache);

                pages.AddRange(results.ToList());
            }

            return pages;
        }
        #endregion
    }
}