using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetTally
{
    public class AdvancedOptions : INotifyPropertyChanged
    {
        #region Lazy singleton creation
        static readonly Lazy<AdvancedOptions> lazy = new Lazy<AdvancedOptions>(() => new AdvancedOptions());

        public static AdvancedOptions Instance => lazy.Value;

        AdvancedOptions()
        {
        }
        #endregion

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

        #region Properties and associated fields
        bool allowRankedVotes = true;
        bool ignoreSymbols = true;
        bool trimExtendedText = false;
        DisplayMode displayMode = DisplayMode.Normal;

        /// <summary>
        /// Whether or not to parse ranked votes in a tally.
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

        /// <summary>
        /// Whether or not to ignore whitespace and symbols when
        /// doing vote and voter comparisons.
        /// </summary>
        public bool IgnoreSymbols
        {
            get { return ignoreSymbols; }
            set
            {
                ignoreSymbols = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to trim extended text from vote lines.
        /// </summary>
        public bool TrimExtendedText
        {
            get { return trimExtendedText; }
            set
            {
                trimExtendedText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enum of the type of display composition methodology to use for the output display.
        /// Recalculates the display if changed.
        /// </summary>
        public DisplayMode DisplayMode
        {
            get { return displayMode; }
            set
            {
                displayMode = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
