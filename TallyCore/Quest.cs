using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally
{
    /// <summary>
    /// The quest class is for storing a quest's thread name, and the starting and
    /// ending posts that are being used to construct a tally.
    /// </summary>
    [DataContract(Name ="Quest")]
    public class Quest : IQuest, INotifyPropertyChanged
    {
        #region Property Changed implementation
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

        #region Variable fields
        static readonly Regex siteRegex = new Regex(@"^(?<siteName>http://[^/]+/)");
        static readonly Regex displayNameRegex = new Regex(@"(?<displayName>[^/]+)(/|#[^/]*)?$");

        public const string NewThreadEntry = "http://forums.sufficientvelocity.com/threads/fake-thread";

        IForumAdapter forumAdapter = null;

        string threadName = NewThreadEntry;
        string displayName = string.Empty;

        int startPost = 1;
        int endPost = 0;
        bool checkForLastThreadmark = false;
        bool useVotePartitions = false;
        bool partitionByLine = true;
        #endregion

        #region IQuest Properties
        public async Task<IForumAdapter> GetForumAdapter(CancellationToken token)
        {
            if (forumAdapter == null)
                forumAdapter = await ForumAdapterFactory.GetAdapter(this, token);
            return forumAdapter;
        }

        public IForumAdapter GetForumAdapter()
        {
            if (forumAdapter == null)
                forumAdapter = ForumAdapterFactory.GetAdapter(this);
            return forumAdapter;
        }

        private void UpdateForumAdapter()
        {
            forumAdapter = ForumAdapterFactory.GetAdapter(this);
        }


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

                string cleanThreadName = CleanPageNumbers(value);

                threadName = cleanThreadName;
                OnPropertyChanged();
                UpdateForumAdapter();
            }
        }

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

                displayName = value;
                OnPropertyChanged();
            }
        }

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
        /// Flag for whether to use vote partitioning when tallying votes.
        /// </summary>
        public bool UseVotePartitions
        {
            get { return useVotePartitions; }
            set
            {
                useVotePartitions = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to use by-line or by-block partitioning,
        /// if partitioning votes during the tally.
        /// </summary>
        public bool PartitionByLine
        {
            get { return partitionByLine; }
            set
            {
                partitionByLine = value;
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
        public bool ReadToEndOfThread => EndPost < 1;
        #endregion


        #region Utility functions
        /// <summary>
        /// Clean page anchors and page numbers from the provided thread URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string CleanPageNumbers(string url)
        {
            if (url == string.Empty)
                return url;

            Regex pageNumberRegex = new Regex(@"^(?<base>.+?)(&?page[-=]?\d+)?(#[^/]*)?$");
            Match m = pageNumberRegex.Match(url);
            if (m.Success)
                url = m.Groups["base"].Value;
            return url;
        }

        public override string ToString()
        {
            if (displayName == string.Empty)
                return ThreadName;
            else
                return DisplayName;
        }
        #endregion

        #region Obsolete Properties

        public const string NewEntryName = "New Entry";
        string site = string.Empty;
        string name = NewEntryName;

        /// <summary>
        /// The name of the web site that the thread is located on.
        /// </summary>
        [Obsolete("Superceded by ThreadName")]
        public string Site
        {
            get { return site; }
            set
            {
                if (value == null)
                    site = string.Empty;
                else
                    site = value;

                forumAdapter = null;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The name of the quest thread.
        /// </summary>
        [Obsolete("Superceded by DisplayName")]
        public string Name
        {
            get { return name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Function to clean up a user-entered name that may contain a web URL.
        /// Example:
        /// http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518
        /// Becomes:
        /// awake-already-homura-nge-pmmm-fusion-quest.11111
        /// </summary>
        /// <returns>Returns just the thread name.</returns>
        [Obsolete("Name is now obsolete")]
        string CleanThreadName(string name)
        {
            //Regex urlRegex = new Regex(@"^(http://forums.sufficientvelocity.com/threads/)?(?<questName>[^/]+)(/.*)?");
            Regex urlRegex = new Regex(@"^((?<siteName>http://[^/]+/)(threads/|forums?/)?)?(?<questName>[^/#]+)");

            var m = urlRegex.Match(name);
            if (m.Success)
            {
                string siteName = m.Groups["siteName"]?.Value;

                if (siteName != null && siteName != string.Empty)
                    Site = siteName;

                return m.Groups["questName"].Value;
            }

            return name;
        }
        #endregion
    }
}