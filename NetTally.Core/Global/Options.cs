using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NetTally.Output;
using NetTally.VoteCounting;

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
            set
            {
                allowRankedVotes = value;
                OnPropertyChanged();
            }
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
            set
            {
                rankVoteCounterMethod = value;
                OnPropertyChanged();
            }
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
            set
            {
                allowVoteLabelPlanNames = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to allow label lines on votes to be plan names.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool ForbidVoteLabelPlanNames
        {
            get { return forbidVoteLabelPlanNames; }
            set
            {
                forbidVoteLabelPlanNames = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to ignore whitespace and symbols when
        /// doing vote and voter comparisons.
        /// </summary>
        [Obsolete("Invert usage")]
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
        /// Whether or not whitespace and punctuation is considered significant when
        /// doing vote and voter comparisons.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return whitespaceAndPunctuationIsSignificant; }
            set
            {
                whitespaceAndPunctuationIsSignificant = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to disable proxy votes (voting for another user to import their vote to your own).
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool DisableProxyVotes
        {
            get { return disableProxyVotes; }
            set
            {
                disableProxyVotes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to force all user proxy votes to be pinned.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool ForcePinnedProxyVotes
        {
            get { return forcePinnedProxyVotes; }
            set
            {
                forcePinnedProxyVotes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to ignore spoiler blocks when parsing.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool IgnoreSpoilers
        {
            get { return ignoreSpoilers; }
            set
            {
                ignoreSpoilers = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether or not to trim extended text from vote lines.
        /// </summary>
        [Obsolete("Moved to Quest")]
        public bool TrimExtendedText
        {
            get { return trimExtendedText; }
            set
            {
                trimExtendedText = value;
                OnPropertyChanged();
            }
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
            set
            {
                displayMode = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to always put spoiler tags around all forms of display output.
        /// </summary>
        public bool GlobalSpoilers
        {
            get { return globalSpoilers; }
            set
            {
                globalSpoilers = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag whether to always put spoiler tags around all forms of display output.
        /// </summary>
        public bool DisplayPlansWithNoVotes
        {
            get { return displayPlansWithNoVotes; }
            set
            {
                displayPlansWithNoVotes = value;
                OnPropertyChanged();
            }
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
                debugMode = value;
                OnPropertyChanged();

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
            set
            {
                disableWebProxy = value;
                OnPropertyChanged();
            }
        }

        #endregion
    }
}
