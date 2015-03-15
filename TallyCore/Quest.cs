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
        //static readonly Regex urlRegex = new Regex(@"^(http://forums.sufficientvelocity.com/threads/)?(?<questName>[^/]+)(/.*)?");
        static readonly Regex urlRegex = new Regex(@"^((?<siteName>http://[^/]+/)(threads/|forums?/)?)?(?<questName>[^/#]+)");
        public const string NewEntryName = "New Entry";
        IForumAdapter forumAdapter = null;

        /// <summary>
        /// Empty constructor for XML serialization.
        /// </summary>
        public Quest() { }

        #region Interface implementations
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

        string site = string.Empty;
        string name = NewEntryName;
        int startPost = 1;
        int endPost = 0;
        bool checkForLastThreadmark = false;
        bool useVotePartitions = false;
        bool partitionByLine = true;

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

        /// <summary>
        /// The name of the web site that the thread is located on.
        /// </summary>
        [DataMember(Order = 0)]
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
        [DataMember(Order = 1)]
        public string Name
        {
            get { return name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (value == string.Empty)
                    throw new ArgumentOutOfRangeException("Quest.Name", "Quest name cannot be set to empty.");

                name = CleanThreadName(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of the post to start looking for votes in.
        /// Not valid below 1.
        /// </summary>
        [DataMember(Order = 2)]
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
        [DataMember(Order = 3)]
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

        /// <summary>
        /// Function to clean up a user-entered name that may contain a web URL.
        /// Example:
        /// http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518
        /// Becomes:
        /// awake-already-homura-nge-pmmm-fusion-quest.11111
        /// </summary>
        /// <returns>Returns just the thread name.</returns>
        string CleanThreadName(string name)
        {
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


        public override string ToString()
        {
            return Name;
        }

    }
}