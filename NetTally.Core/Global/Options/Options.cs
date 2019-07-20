using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Output;
using NetTally.VoteCounting;

namespace NetTally.Options
{
    public class AdvancedOptions : INotifyPropertyChanged, IGeneralInputOptions, IGeneralOutputOptions, IGlobalOptions
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

        readonly Stack<bool> dirty = new Stack<bool>();
        readonly Stack<string> propertyNames = new Stack<string>();

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected async void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            dirty.Push(true);
            propertyNames.Push(propertyName);

            await Task.Delay(25);

            if (dirty.Pop() && dirty.Count == 0)
            {
                foreach (var name in propertyNames)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

                propertyNames.Clear();
            }
        }
        #endregion

        #region Properties and associated fields
        DisplayMode displayMode = DisplayMode.Normal;

        bool allowRankedVotes = true;
        RankVoteCounterMethod rankVoteCounterMethod = RankVoteCounterMethod.Default;

        [Obsolete("Invert usage")]
        bool allowVoteLabelPlanNames = true;
        bool forbidVoteLabelPlanNames = false;
        [Obsolete("Invert usage")]
        bool ignoreSymbols = true;
        bool whitespaceAndPunctuationIsSignificant = false;
        bool disableProxyVotes = false;
        bool forcePinnedProxyVotes = false;
        bool ignoreSpoilers = false;
        bool trimExtendedText = false;

        bool globalSpoilers = false;
        bool displayPlansWithNoVotes = false;

        bool disableWebProxy = false;
        bool debugMode = false;
        #endregion

        #region Constants for string descriptions
        public const string _displayMode = "displayMode";
        public const string _allowRankedVotes = "allowRankedVotes";
        public const string _rankVoteCounterMethod = "rankVoteCounterMethod";
        public const string _forbidVoteLabelPlanNames = "forbidVoteLabelPlanNames";
        public const string _whitespaceAndPunctuationIsSignificant = "whitespaceAndPunctuationIsSignificant";
        public const string _caseIsSignificant = "caseIsSignificant";
        public const string _disableProxyVotes = "disableProxyVotes";
        public const string _forcePinnedProxyVotes = "forcePinnedProxyVotes";
        public const string _ignoreSpoilers = "ignoreSpoilers";
        public const string _trimExtendedText = "trimExtendedText";
        public const string _globalSpoilers = "globalSpoilers";
        public const string _displayPlansWithNoVotes = "displayPlansWithNoVotes";
        public const string _debugMode = "debugMode";
        public const string _disableWebProxy = "disableWebProxy";
        #endregion


        #region General Options
        /// <summary>
        /// Whether or not to parse ranked votes in a tally.
        /// </summary>
        public bool AllowRankedVotes
        {
            get { return allowRankedVotes; }
            set { SetProperty(ref allowRankedVotes, value); }
        }

        /// <summary>
        /// Gets or sets the rank vote counter method.
        /// </summary>
        /// <value>
        /// The rank vote counter method.
        /// </value>
        public RankVoteCounterMethod RankVoteCounterMethod
        {
            get { return rankVoteCounterMethod; }
            set { SetProperty(ref rankVoteCounterMethod, value); }
        }
        #endregion

        #region Formatting Options
        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        [Obsolete("Invert usage")]
        public bool AllowVoteLabelPlanNames
        {
            get { return allowVoteLabelPlanNames; }
            set { SetProperty(ref allowVoteLabelPlanNames, value); }
        }

        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool ForbidVoteLabelPlanNames
        {
            get { return forbidVoteLabelPlanNames; }
            set { SetProperty(ref forbidVoteLabelPlanNames, value); }
        }

        /// <summary>
        /// Whether or not to ignore whitespace and symbols when
        /// doing vote and voter comparisons.
        /// </summary>
        [Obsolete("Invert usage")]
        public bool IgnoreSymbols
        {
            get { return ignoreSymbols; }
            set { SetProperty(ref ignoreSymbols, value); }
        }

        /// <summary>
        /// Whether or not whitespace and punctuation is considered significant when
        /// doing vote and voter comparisons.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return whitespaceAndPunctuationIsSignificant; }
            set { SetProperty(ref whitespaceAndPunctuationIsSignificant, value); }
        }

        /// <summary>
        /// Flag whether to disable proxy votes (voting for another user to import their vote to your own).
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool DisableProxyVotes
        {
            get { return disableProxyVotes; }
            set { SetProperty(ref disableProxyVotes, value); }
        }

        /// <summary>
        /// Flag whether to force all user proxy votes to be pinned.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool ForcePinnedProxyVotes
        {
            get { return forcePinnedProxyVotes; }
            set { SetProperty(ref forcePinnedProxyVotes, value); }
        }

        /// <summary>
        /// Whether or not to ignore spoiler blocks when parsing.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool IgnoreSpoilers
        {
            get { return ignoreSpoilers; }
            set { SetProperty(ref ignoreSpoilers, value); }
        }

        /// <summary>
        /// Whether or not to trim extended text from vote lines.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool TrimExtendedText
        {
            get { return trimExtendedText; }
            set { SetProperty(ref trimExtendedText, value); }
        }
        #endregion

        #region Output Options
        /// <summary>
        /// Enum of the type of display composition methodology to use for the output display.
        /// Recalculates the display if changed.
        /// </summary>
        public DisplayMode DisplayMode
        {
            get { return displayMode; }
            set { SetProperty(ref displayMode, value); }
        }

        /// <summary>
        /// Flag whether to always put spoiler tags around all forms of display output.
        /// </summary>
        public bool GlobalSpoilers
        {
            get { return globalSpoilers; }
            set { SetProperty(ref globalSpoilers, value); }
        }

        /// <summary>
        /// Flag whether to always put spoiler tags around all forms of display output.
        /// </summary>
        public bool DisplayPlansWithNoVotes
        {
            get { return displayPlansWithNoVotes; }
            set { SetProperty(ref displayPlansWithNoVotes, value); }
        }
        #endregion

        #region Miscellaneous Options
        /// <summary>
        /// Whether the program should be running in its debug mode.
        /// </summary>
        public bool DebugMode
        {
            get { return debugMode; }
            set
            {
                if (debugMode != value)
                {
                    debugMode = value;
                    OnPropertyChanged();
                }

#if DEBUG
                if (debugMode)
                    Logger.LoggingLevel = LoggingLevel.Info;
#else
                if (debugMode)
                    Logger.LoggingLevel = LoggingLevel.Warning;
#endif
                else
                    Logger.LoggingLevel = LoggingLevel.Error;
            }
        }

        /// <summary>
        /// Disable use of local proxy searches when loading web pages.
        /// </summary>
        public bool DisableWebProxy
        {
            get { return disableWebProxy; }
            set { SetProperty(ref disableWebProxy, value); }
        }

        #endregion

        #region Generic Property Setting
        /// <summary>
        /// Generic handling to set property values and raise the property changed event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage">A reference to the backing property being set.</param>
        /// <param name="value">The value to be stored.</param>
        /// <param name="propertyName">Name of the property being set.</param>
        /// <returns>Returns true if the value was updated, or false if no change was made.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (storage is IComparable<T> comparableStorage && comparableStorage.CompareTo(value) == 0)
            {
                return false;
            }
            else if (Object.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }
        #endregion

    }
}
