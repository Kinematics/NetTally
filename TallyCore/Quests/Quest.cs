using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

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

        IForumAdapter forumAdapter = null;

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
        /// Get the URL string for the provided page number of the quest thread.
        /// </summary>
        /// <param name="pageNumber">The page number to get.</param>
        /// <returns>Returns a URL of the forum thread for the apge number.</returns>
        public string GetPageUrl(int pageNumber)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            return forumAdapter?.GetPageUrl(ThreadName, pageNumber);
        }

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
        public async Task<int> GetFirstPageNumber(IPageProvider pageProvider, CancellationToken token)
        {
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            IForumAdapter adapter = await GetForumAdapterAsync(token);

            if (adapter == null)
                throw new ApplicationException($"Unable to acquire a valid forum adapter for quest {DisplayName}.");

            int startPostNumber = await adapter.GetStartingPostNumber(pageProvider, this, token);

            return GetPageNumberOf(startPostNumber);
        }

        /// <summary>
        /// Get the last page number of the thread, where we should stop reading, based on
        /// current quest parameters and the provided web page.
        /// </summary>
        /// <param name="loadedPage">A page loaded from the thread, that we can check for page info.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the last page number we should try to read for the tally.</returns>
        public async Task<int> GetLastPageNumber(HtmlDocument loadedPage, CancellationToken token)
        {
            if (loadedPage == null)
                throw new ArgumentNullException(nameof(loadedPage));

            if (ReadToEndOfThread)
            {
                IForumAdapter adapter = await GetForumAdapterAsync(token);

                if (adapter == null)
                    throw new ApplicationException($"Unable to acquire a valid forum adapter for quest {DisplayName}.");

                return adapter.GetLastPageNumberOfThread(loadedPage);
            }
            else
            {
                return GetPageNumberOf(EndPost);
            }
        }

        #endregion

        #region IQuest Properties
        /// <summary>
        /// Gets the expected forum adapter for this quest, with option to cancel.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public async Task<IForumAdapter> GetForumAdapterAsync(CancellationToken token)
        {
            if (forumAdapter == null)
            {
                forumAdapter = await ForumAdapterFactory.GetAdapter(this, token).ConfigureAwait(false);
                if (postsPerPage == 0)
                    PostsPerPage = forumAdapter.DefaultPostsPerPage;
            }
            return forumAdapter;
        }

        /// <summary>
        /// Gets the expected forum adapter for this quest.
        /// </summary>
        /// <returns>Returns an IForumAdapter to read the quest thread.</returns>
        public IForumAdapter GetForumAdapter()
        {
            if (forumAdapter == null)
            {
                forumAdapter = ForumAdapterFactory.GetAdapter(this);
            }
            return forumAdapter;
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
                    var ppp = GetForumAdapter()?.DefaultPostsPerPage;
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
        private void UpdateForumAdapter()
        {
            var prevForumAdapter = forumAdapter;

            forumAdapter = ForumAdapterFactory.GetAdapter(this);

            if (prevForumAdapter != null && prevForumAdapter != forumAdapter)
                UpdatePostsPerPage();
        }

        /// <summary>
        /// Update the posts per page if the forum adapter was modified.
        /// </summary>
        private void UpdatePostsPerPage()
        {
            var ppp = forumAdapter?.DefaultPostsPerPage;

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
    }
}