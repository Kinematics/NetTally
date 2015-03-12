using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;

namespace NetTally
{
    /// <summary>
    /// The quest class is for storing a quest's thread name, and the starting and
    /// ending posts that are being used to construct a tally.
    /// </summary>
    public class Quest : IQuest, INotifyPropertyChanged
    {
        static readonly Regex urlRegex = new Regex(@"^(http://forums.sufficientvelocity.com/threads/)?(?<questName>[^/]+)(/.*)?");

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

        #region Properties
        string name = "New Entry";
        /// <summary>
        /// The name of the quest thread.
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (value == string.Empty)
                    throw new ArgumentOutOfRangeException("Quest.Name", "Quest name cannot be set to empty.");
                name = Clean(value);
                OnPropertyChanged();
            }
        }

        int startPost = 1;
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

        int endPost = 0;
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
        /// Boolean value indicating if the tally system should read to the end
        /// of the thread.  This is done when the EndPost is 0.
        /// </summary>
        public bool ReadToEndOfThread => EndPost < 1;

        /// <summary>
        /// Function to clean up a user-entered name that may contain a web URL.
        /// Example:
        /// http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518
        /// Becomes:
        /// awake-already-homura-nge-pmmm-fusion-quest.11111
        /// </summary>
        string Clean(string name)
        {
            var m = urlRegex.Match(name);
            if (m.Success)
            {
                return m.Groups["questName"].Value;
            }

            return name;
        }
        #endregion


        public override string ToString()
        {
            return Name;
        }

    }
}