using System.Configuration;
using System.Linq;
using NetTally.Types.Enums;

namespace NetTally.Systems.Config.Xml
{
    /// <summary>
    /// Class for individual quest entries to be added to the user config file.
    /// </summary>
    public class QuestElement : ConfigurationElement
    {
        #region Deprecation handling
        readonly string[] deprecatedAttributes = ["UseVotePartitions", "PartitionByLine", "AllowRankedVotes"];

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            if (deprecatedAttributes.Contains(name))
            {
                return true;
            }

            if (ConfigPrefs.Strict)
                return base.OnDeserializeUnrecognizedAttribute(name, value);
            else
                return true;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QuestElement"/> class.
        /// </summary>
        public QuestElement()
        {
        }
        #endregion

        #region Properties
        [ConfigurationProperty("ThreadName", DefaultValue = "", IsKey = true)]
        public string ThreadName
        {
            get { return (string)this[nameof(ThreadName)]; }
            set { this[nameof(ThreadName)] = value; }
        }

        [ConfigurationProperty("DisplayName", DefaultValue = "")]
        public string DisplayName
        {
            get { return (string)this[nameof(DisplayName)]; }
            set { this[nameof(DisplayName)] = value; }
        }

        [ConfigurationProperty("PostsPerPage", DefaultValue = 0)]
        public int PostsPerPage
        {
            get { return (int)this[nameof(PostsPerPage)]; }
            set { this[nameof(PostsPerPage)] = value; }
        }

        [ConfigurationProperty("StartPost", DefaultValue = 1, IsRequired = true)]
        public int StartPost
        {
            get { return (int)this[nameof(StartPost)]; }
            set { this[nameof(StartPost)] = value; }
        }

        [ConfigurationProperty("EndPost", DefaultValue = 0, IsRequired = true)]
        public int EndPost
        {
            get { return (int)this[nameof(EndPost)]; }
            set { this[nameof(EndPost)] = value; }
        }

        [ConfigurationProperty("CheckForLastThreadmark", DefaultValue = false)]
        public bool CheckForLastThreadmark
        {
            get { return (bool)this[nameof(CheckForLastThreadmark)]; }
            set { this[nameof(CheckForLastThreadmark)] = value; }
        }

        [ConfigurationProperty("PartitionMode", DefaultValue = PartitionMode.None)]
        public PartitionMode PartitionMode
        {
            get
            {
                try
                {
                    return (PartitionMode)this[nameof(PartitionMode)];
                }
                catch (ConfigurationException)
                {
                    return PartitionMode.None;
                }
            }
            set { this[nameof(PartitionMode)] = value; }
        }

        [ConfigurationProperty("UseCustomThreadmarkFilters", DefaultValue = false)]
        public bool UseCustomThreadmarkFilters
        {
            get { return (bool)this[nameof(UseCustomThreadmarkFilters)]; }
            set { this[nameof(UseCustomThreadmarkFilters)] = value; }
        }

        [ConfigurationProperty("CustomThreadmarkFilters", DefaultValue = "")]
        public string CustomThreadmarkFilters
        {
            get { return (string)this[nameof(CustomThreadmarkFilters)]; }
            set { this[nameof(CustomThreadmarkFilters)] = value; }
        }

        [ConfigurationProperty("UseCustomUsernameFilters", DefaultValue = false)]
        public bool UseCustomUsernameFilters
        {
            get { return (bool)this[nameof(UseCustomUsernameFilters)]; }
            set { this[nameof(UseCustomUsernameFilters)] = value; }
        }

        [ConfigurationProperty("CustomUsernameFilters", DefaultValue = "")]
        public string CustomUsernameFilters
        {
            get { return (string)this[nameof(CustomUsernameFilters)]; }
            set { this[nameof(CustomUsernameFilters)] = value; }
        }

        [ConfigurationProperty("UseCustomPostFilters", DefaultValue = false)]
        public bool UseCustomPostFilters
        {
            get { return (bool)this[nameof(UseCustomPostFilters)]; }
            set { this[nameof(UseCustomPostFilters)] = value; }
        }

        [ConfigurationProperty("CustomPostFilters", DefaultValue = "")]
        public string CustomPostFilters
        {
            get { return (string)this[nameof(CustomPostFilters)]; }
            set { this[nameof(CustomPostFilters)] = value; }
        }

        [ConfigurationProperty("WhitespaceAndPunctuationIsSignificant", DefaultValue = false)]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return (bool)this[nameof(WhitespaceAndPunctuationIsSignificant)]; }
            set { this[nameof(WhitespaceAndPunctuationIsSignificant)] = value; }
        }

        [ConfigurationProperty("CaseIsSignificant", DefaultValue = false)]
        public bool CaseIsSignificant
        {
            get { return (bool)this[nameof(CaseIsSignificant)]; }
            set { this[nameof(CaseIsSignificant)] = value; }
        }

        [ConfigurationProperty("ForcePlanReferencesToBeLabeled", DefaultValue = false)]
        public bool ForcePlanReferencesToBeLabeled
        {
            get { return (bool)this[nameof(ForcePlanReferencesToBeLabeled)]; }
            set { this[nameof(ForcePlanReferencesToBeLabeled)] = value; }
        }

        [ConfigurationProperty("ForbidVoteLabelPlanNames", DefaultValue = false)]
        public bool ForbidVoteLabelPlanNames
        {
            get { return (bool)this[nameof(ForbidVoteLabelPlanNames)]; }
            set { this[nameof(ForbidVoteLabelPlanNames)] = value; }
        }

        [ConfigurationProperty("AllowUsersToUpdatePlans", DefaultValue = false)]
        public bool AllowUsersToUpdatePlans
        {
            get { return (bool)this[nameof(AllowUsersToUpdatePlans)]; }
            set { this[nameof(AllowUsersToUpdatePlans)] = value; }
        }

        [ConfigurationProperty("DisableProxyVotes", DefaultValue = false)]
        public bool DisableProxyVotes
        {
            get { return (bool)this[nameof(DisableProxyVotes)]; }
            set { this[nameof(DisableProxyVotes)] = value; }
        }

        [ConfigurationProperty("ForcePinnedProxyVotes", DefaultValue = false)]
        public bool ForcePinnedProxyVotes
        {
            get { return (bool)this[nameof(ForcePinnedProxyVotes)]; }
            set { this[nameof(ForcePinnedProxyVotes)] = value; }
        }

        [ConfigurationProperty("IgnoreSpoilers", DefaultValue = false)]
        public bool IgnoreSpoilers
        {
            get { return (bool)this[nameof(IgnoreSpoilers)]; }
            set { this[nameof(IgnoreSpoilers)] = value; }
        }

        [ConfigurationProperty("TrimExtendedText", DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this[nameof(TrimExtendedText)]; }
            set { this[nameof(TrimExtendedText)] = value; }
        }

        [ConfigurationProperty("UseRSSThreadmarks", DefaultValue = BoolEx.Unknown)]
        public BoolEx UseRSSThreadmarks
        {
            get
            {
                try
                {
                    return (BoolEx)this[nameof(UseRSSThreadmarks)];
                }
                catch (ConfigurationException)
                {
                    return BoolEx.Unknown;
                }
            }
            set { this[nameof(UseRSSThreadmarks)] = value; }
        }

        [ConfigurationProperty("LinkedQuests", DefaultValue = "")]
        public string LinkedQuests
        {
            get { return (string)this[nameof(LinkedQuests)]; }
            set { this[nameof(LinkedQuests)] = value; }
        }
        #endregion
    }
}
