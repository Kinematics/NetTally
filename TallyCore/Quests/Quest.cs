using System;
using System.Text.RegularExpressions;
using NetTally.Adapters;
using NetTally.Extensions;
using NetTally.Utility;
using NetTally.Votes;
using NetTally.Web;

namespace NetTally
{
    /// <summary>
    /// A Quest is a named forum thread, along with all the configuration properties that
    /// are used to determine how to go about tallying that thread.
    /// </summary>
    public partial class Quest : IQuest
    {
        public const string OmakeFilter = @"\bomake\b";

        public Quest()
        {
            questHash = indexer.Next();
            ThreadName = NewThreadEntry;
            CustomThreadmarkFilters = string.Empty;
            CustomTaskFilters = string.Empty;
        }

        #region Hashing
        // Quest hash is used to set the hash code for this object.
        // Since all other intrinsic values are mutable, it is set to 
        // an immutable random value.
        static Random indexer = new Random();
        private readonly int questHash;
        #endregion

        #region URL and Display Name
        string threadName = string.Empty;
        string displayName = string.Empty;
        static readonly Regex pageNumberRegex = new Regex(@"^(?<base>.+?)(&?page[-=]?\d+)?(&p=?\d+)?(#[^/]*)?$");
        static readonly Regex displayNameRegex = new Regex(@"(?<displayName>[^/]+)(/|#[^/]*)?$");
        public const string NewThreadEntry = "https://forums.sufficientvelocity.com/threads/fake-thread.00000";

        /// <summary>
        /// The URI that represents the thread URL string.
        /// </summary>
        public Uri ThreadUri { get; private set; }

        /// <summary>
        /// The type of forum used at the URI site.
        /// </summary>
        public ForumType ForumType { get; set; }

        /// <summary>
        /// Get the forum adapter being used by this quest.
        /// Gets set when the ForumType is determined.
        /// </summary>
        public IForumAdapter ForumAdapter { get; set; }

        /// <summary>
        /// The URL of the quest.
        /// Cannot be set to null or an empty string, and must be a well-formed URL.
        /// Automatically removes unsafe characters, and navigation elements from the URL.
        /// </summary>
        public string ThreadName
        {
            get => threadName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("URL cannot be null or empty.", nameof(value));
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                    throw new ArgumentException($"URL ({value}) is not well formed.", nameof(value));

                string cleanValue = CleanupThreadName(value);

                Uri newUri = new Uri(cleanValue);

                if (ThreadUri == null || ThreadUri.Host != newUri.Host)
                {
                    ForumType = ForumType.Unknown;
                    ForumAdapter = null;
                }

                string oldThreadName = threadName;

                threadName = cleanValue;
                ThreadUri = newUri;

                OnPropertyChanged();

                // Reset the display name if it's based on the URL.
                if (string.IsNullOrEmpty(displayName))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
                else
                {
                    if (displayName == GetDisplayNameFromUrl(oldThreadName))
                        DisplayName = "";
                    else if (displayName == GetDisplayNameFromUrl(threadName))
                        displayName = "";
                }
            }
        }

        /// <summary>
        /// The friendly display name to show for the quest.
        /// If the backing var is empty, or if an attempt is made to set it to an empty value,
        /// automatically generates a value based on the thread URL.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    return GetDisplayNameFromThreadName();
                }

                return displayName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    displayName = value;
                else
                    displayName = value.RemoveUnsafeCharacters();

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Shorthand function to get the display name for the current thread name.
        /// </summary>
        /// <returns>Returns a display name based on the current thread name.</returns>
        private string GetDisplayNameFromThreadName()
        {
            return GetDisplayNameFromUrl(ThreadName);
        }

        /// <summary>
        /// Function to extract a display name from the provided thread name, if possible.
        /// If it fails, just returns the entire URL.
        /// </summary>
        /// <param name="url">The URL to extract a display name out of.</param>
        /// <returns>Returns a name based on the provided URL.</returns>
        private string GetDisplayNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            Match m = displayNameRegex.Match(url);
            if (m.Success)
                return m.Groups["displayName"].Value;
            else
                return url;
        }

        /// <summary>
        /// Remove unsafe characters from the provided URL, and strip navigation elements from the end.
        /// </summary>
        /// <param name="url">The URL to clean up.</param>
        /// <returns>Returns the base URL without navigation elements.</returns>
        private string CleanupThreadName(string url)
        {
            url = url.RemoveUnsafeCharacters();

            Match m = pageNumberRegex.Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;

            return url;
        }
        #endregion

        #region Quest Configuration Properties
        #region Quest configuration properties: Post numbers
        int postsPerPage = 0;
        int startPost = 1;
        int endPost = 0;
        bool checkForLastThreadmark = true;

        /// <summary>
        /// The number of the post to start looking for votes in.
        /// Must be a value greater than or equal to 1.
        /// </summary>
        public int StartPost
        {
            get { return startPost; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(StartPost), "Starting post number must be at least 1.");
                startPost = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of the last post to look for votes in.
        /// Must be a value greater than or equal to 0.
        /// A value of 0 means it reads to the end of the thread.
        /// </summary>
        public int EndPost
        {
            get { return endPost; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(EndPost), "Ending post number must be at least 0.");
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
        public bool ReadToEndOfThread => EndPost == 0;

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
        /// Converts a post number into a page number.
        /// </summary>
        /// <param name="postNumber">The post number of the thread.</param>
        /// <returns>Returns the page number of the thread that the post should be on.</returns>
        public int GetPageNumberOf(int postNumber) => ((postNumber - 1) / PostsPerPage) + 1;

        #endregion

        #region Quest configuration properties: Filtering
        bool useCustomThreadmarkFilters = false;
        string customThreadmarkFilters = string.Empty;
        bool customThreadmarkFiltersAreRegex = false;

        bool useCustomTaskFilters = false;
        string customTaskFilters = string.Empty;
        bool customTaskFiltersAreRegex = false;

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
                ThreadmarkFilter = new Filter(customThreadmarkFilters, OmakeFilter, customThreadmarkFiltersAreRegex);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether the custom filters used for the threadmark filter are a regex string.
        /// </summary>
        public bool CustomThreadmarkFiltersAreRegex
        {
            get { return customThreadmarkFiltersAreRegex; }
            set
            {
                customThreadmarkFiltersAreRegex = value;
                ThreadmarkFilter = new Filter(customThreadmarkFilters, OmakeFilter, customThreadmarkFiltersAreRegex);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the threadmark filter, based on current threadmark filter settings.
        /// </summary>
        public Filter ThreadmarkFilter { get; private set; }

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
                TaskFilter = new Filter(customTaskFilters, null, customTaskFiltersAreRegex);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether the custom filters used for the task filter are a regex string.
        /// </summary>
        public bool CustomTaskFiltersAreRegex
        {
            get { return customTaskFiltersAreRegex; }
            set
            {
                customTaskFiltersAreRegex = value;
                TaskFilter = new Filter(customTaskFilters, null, customTaskFiltersAreRegex);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the task filter, based on current task filter settings.
        /// </summary>
        public Filter TaskFilter { get; private set; }

        #endregion

        #region Quest configuration properties: Tally processing
        PartitionMode partitionMode = PartitionMode.None;

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
        #endregion
        #endregion

        #region Object overrides
        public override string ToString()
        {
            return DisplayName;
        }
        #endregion
    }
}
