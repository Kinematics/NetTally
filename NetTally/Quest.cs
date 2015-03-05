using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace NetTally
{
    public class Quest : INotifyPropertyChanged
    {
        public Quest() { }

        public Quest(string name, int start, int end)
        {
            Name = name;
            StartPost = start;
            EndPost = end;
        }

        #region Interface implementations
        /// <summary>
        /// INotifyPropertyChanged
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
        string name = string.Empty;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        int startPost = 0;

        public int StartPost
        {
            get { return startPost; }
            set
            {
                startPost = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        int endPost = 0;

        public int EndPost
        {
            get { return endPost; }
            set
            {
                endPost = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Function to clean up a possible HTML-form name.
        /// Example:
        /// http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518
        /// </summary>
        internal void CleanName()
        {
            if (Name != null)
            {
                Regex urlRegex = new Regex(@"^(http://forums.sufficientvelocity.com/threads/)?(?<questName>[^/]+)(/.*)?");
                var m = urlRegex.Match(Name);
                if (m.Success)
                {
                    Name = m.Groups["questName"].Value;
                }
            }
        }
        #endregion
    }


    /// <summary>
    /// Converter class for data binding, to allow conversion between the name of a
    /// quest and the quest object itself.
    /// </summary>
    public class QuestConverter : IValueConverter
    {
        /// <summary>
        /// Convert from source (object) to target (string).
        /// </summary>
        /// <returns>Returns the name of the quest object for display.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var q = value as Quest;
            if (q != null)
                return q.Name;

            return string.Empty;
        }

        /// <summary>
        /// Convert from target (string) to source (object).
        /// </summary>
        /// <returns>Returns the object that corresponds to the provide quest name.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Quests.GetQuest((string)value);
        }
    }

}