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
    public class Quest : INotifyPropertyChanged
    {
        static Regex urlRegex = new Regex(@"^(http://forums.sufficientvelocity.com/threads/)?(?<questName>[^/]+)(/.*)?");

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
                name = value;
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
                endPost = value;
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
        internal void CleanName()
        {
            var m = urlRegex.Match(Name);
            if (m.Success)
            {
                Name = m.Groups["questName"].Value;
            }
        }
        #endregion
    }
}