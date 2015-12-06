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

namespace NetTally
{
    /// <summary>
    /// The quest class is for storing a quest's thread name, and the starting and
    /// ending posts that are being used to construct a tally.
    /// </summary>
    [DataContract(Name ="Quest")]
    public class Quest : IQuest, INotifyPropertyChanged
    {
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

        public const string NewThreadEntry = "http://forums.sufficientvelocity.com/threads/fake-thread";

        public IForumAdapter2 ForumAdapter { get; private set; } = null;


        string threadName = NewThreadEntry;
        string displayName = string.Empty;

        int postsPerPage = 0;

        int startPost = 1;
        int endPost = 0;

        bool checkForLastThreadmark = false;
        bool allowRankedVotes = false;
        PartitionMode partitionMode = PartitionMode.None;
        #endregion

        #region Page Stuff
        /// <summary>
        /// Converts a post number into a page number.
        /// </summary>
        /// <param name="postNumber">The post number of the thread.</param>
        /// <returns>Returns the page number of the thread that the post should be on.</returns>
        public int GetPageNumberOf(int postNumber) => ((postNumber - 1) / PostsPerPage) + 1;

        /// <summary>
        /// Get the first page number of the thread, where we should start reading, based on
        /// current quest parameters.  Forum adapter handles checking for threadmarks and such.
        /// </summary>
        /// <param name="pageProvider">The page provider that can be used to load web pages.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the number of the first page we should start loading.</returns>
        public async Task<ThreadStartValue> GetStartInfo(IPageProvider pageProvider, CancellationToken token)
        {
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            var startInfo = await ForumAdapter.GetStartingPostNumber(this, pageProvider, token);

            return startInfo;
        }

        #endregion

        #region IQuest Properties
        /// <summary>
        /// Gets the expected forum adapter for this quest.
        /// </summary>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public async Task InitForumAdapter() => await InitForumAdapter(CancellationToken.None);

        /// <summary>
        /// Gets the expected forum adapter for this quest, with option to cancel.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public async Task InitForumAdapter(CancellationToken token)
        {
            if (ForumAdapter == null)
            {
                ForumAdapter = await ForumAdapterFactory2.GetAdapter(this, token).ConfigureAwait(false);

                if (ForumAdapter == null)
                    throw new InvalidOperationException();

                if (postsPerPage == 0)
                    PostsPerPage = ForumAdapter.DefaultPostsPerPage;
            }
        }

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
                    throw new ArgumentNullException("ThreadName");
                if (value == string.Empty)
                    throw new ArgumentOutOfRangeException("ThreadName", "Thread name cannot be empty.");

                threadName = CleanPageNumbers(value);

                OnPropertyChanged();
                OnPropertyChanged("DisplayName");

                ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateForumAdapter));
            }
        }

        /// <summary>
        /// Gets the name to display in the combo box dropdown, for selecting a quest.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (displayName != string.Empty)
                    return displayName;

                Match m = displayNameRegex.Match(threadName);
                if (m.Success)
                    return m.Groups["displayName"].Value;

                return threadName;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("DisplayName");

                displayName = Utility.Text.SafeString(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the web site name from the thread URL.
        /// </summary>
        public string SiteName
        {
            get
            {
                Match m = siteRegex.Match(threadName);
                if (m.Success)
                    return m.Groups["siteName"].Value;

                // Default site if no site given in thread name.
                return "http://forums.sufficientvelocity.com/";
            }
        }

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
        /// Get the number of posts per page for this forum thread.
        /// Raw value, without attempt at auto-fill.
        /// </summary>
        public int RawPostsPerPage
        {
            get { return postsPerPage; }
            set { postsPerPage = value; }
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
                    throw new ArgumentOutOfRangeException("Start Post", "Start post must be at least 1.");
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
                    throw new ArgumentOutOfRangeException("End Post", "End post must be at least 0.");
                endPost = value;
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
        public bool ReadToEndOfThread => EndPost < 1 || ThreadmarkPost > 0;

        /// <summary>
        /// Property to store any found threadmark post number.
        /// </summary>
        public int ThreadmarkPost { get; set; } = 0;

        /// <summary>
        /// Return either the StartPost or the ThreadmarkPost, depending on config.
        /// </summary>
        public int FirstTallyPost
        {
            get
            {
                if (CheckForLastThreadmark && ThreadmarkPost > 0)
                    return ThreadmarkPost;
                else
                    return StartPost;
            }
        }

        /// <summary>
        /// Flag for whether to count votes using preferential vote ranking.
        /// </summary>
        public bool AllowRankedVotes
        {
            get { return allowRankedVotes; }
            set
            {
                allowRankedVotes = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Utility functions
        /// <summary>
        /// Call this if anything changes the thread name, as that means the forum adapter is now invalid.
        /// </summary>
        private async void UpdateForumAdapter(object stateInfo)
        {
            var prevForumAdapter = ForumAdapter;
            var prevHost = ForumAdapter?.Site?.Host;

            await InitForumAdapter().ConfigureAwait(false);

            ForumAdapter.Site = new Uri(ThreadName);

            if (prevHost == null || prevHost != ForumAdapter?.Site?.Host)
                UpdatePostsPerPage();
        }

        /// <summary>
        /// Update the posts per page if the forum adapter was modified.
        /// </summary>
        private void UpdatePostsPerPage()
        {
            var ppp = ForumAdapter?.DefaultPostsPerPage;

            if (ppp.HasValue)
            {
                PostsPerPage = ppp.Value;
            }
            else
            {
                PostsPerPage = 0;
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

            return Utility.Text.SafeString(url);
        }

        /// <summary>
        /// Override ToString for class.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public override string ToString()
        {
            if (displayName == string.Empty)
                return ThreadName;
            else
                return DisplayName;
        }
        #endregion


        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="quest">Quest object containing query parameters.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<HtmlDocument>> LoadQuestPages(IPageProvider pageProvider, CancellationToken token)
        {
            try
            {
                var startInfo = await GetStartInfo(pageProvider, token);

                int firstPageNumber = 0;
                if (startInfo.ByNumber)
                    firstPageNumber = GetPageNumberOf(startInfo.Number);
                else
                    firstPageNumber = startInfo.Page;

                if (startInfo.Number == 1)
                    ThreadmarkPost = 1;
                else if (!startInfo.ByNumber)
                    ThreadmarkPost = startInfo.ID;
                else
                    ThreadmarkPost = 0;

                HtmlDocument firstPage = await pageProvider.GetPage(ForumAdapter.GetUrlForPage(firstPageNumber),
                    $"Page {firstPageNumber}", Caching.BypassCache, token).ConfigureAwait(false);

                if (firstPage == null)
                    throw new InvalidOperationException("Unable to load web page.");

                // We will store the loaded pages in a new List.
                List<HtmlDocument> pages = new List<HtmlDocument>();
                pages.Add(firstPage);

                ThreadInfo threadInfo = ForumAdapter.GetThreadInfo(firstPage);

                // Set parameters for which pages to try to load
                int pagesToScan = threadInfo.Pages - firstPageNumber;

                // Initiate the async tasks to load the pages
                if (pagesToScan > 0)
                {
                    // Initiate tasks for all pages other than the first page (which we already loaded)
                    var results = from pageNum in Enumerable.Range(firstPageNumber + 1, pagesToScan)
                                  let cacheMode = pageNum == threadInfo.Pages ? Caching.BypassCache : Caching.UseCache
                                  let pageUrl = ForumAdapter.GetUrlForPage(pageNum)
                                  select pageProvider.GetPage(pageUrl, $"Page {pageNum}", cacheMode, token);

                    // Wait for all the tasks to be completed.
                    HtmlDocument[] pageArray = await Task.WhenAll(results).ConfigureAwait(false);

                    if (pageArray.Any(p => p == null))
                    {
                        throw new ApplicationException("Not all pages loaded.  Rerun tally.");
                    }

                    // Add the results to our list of pages.
                    pages.AddRange(pageArray);
                }

                return pages;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }

    }
}