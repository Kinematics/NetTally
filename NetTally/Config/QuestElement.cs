using System.Configuration;
using System.Linq;
using NetTally.Votes;

namespace NetTally
{
    /// <summary>
    /// Class for individual quest entries to be added to the user config file.
    /// </summary>
    public class QuestElement : ConfigurationElement
    {
        string[] deprecatedAttributes = new string[] { "UseVotePartitions", "PartitionByLine", "AllowRankedVotes" };


        public QuestElement(string threadName, string displayName, int postsPerPage, int startPost, int endPost, bool checkForLastThreadmark,
            PartitionMode partitionMode, bool useCustomThreadmarkFilters, string customThreadmarkFilters, bool useCustomUsernameFilters,
            string customUsernameFilters, bool useCustomPostFilters, string customPostFilters)
        {
            ThreadName = threadName;
            DisplayName = displayName;
            PostsPerPage = postsPerPage;
            StartPost = startPost;
            EndPost = endPost;
            CheckForLastThreadmark = checkForLastThreadmark;
            PartitionMode = partitionMode;
            UseCustomThreadmarkFilters = useCustomThreadmarkFilters;
            CustomThreadmarkFilters = customThreadmarkFilters;
            UseCustomUsernameFilters = useCustomUsernameFilters;
            CustomUsernameFilters = customUsernameFilters;
            UseCustomPostFilters = useCustomPostFilters;
            CustomPostFilters = customPostFilters;
        }

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


        /// <summary>
        /// Initializes a new instance of the <see cref="QuestElement"/> class.
        /// </summary>
        public QuestElement()
        { }


        [ConfigurationProperty("ThreadName", DefaultValue = "", IsKey = true)]
        public string ThreadName
        {
            get { return (string)this["ThreadName"]; }
            set { this["ThreadName"] = value; }
        }

        [ConfigurationProperty("DisplayName", DefaultValue = "")]
        public string DisplayName
        {
            get { return (string)this["DisplayName"]; }
            set { this["DisplayName"] = value; }
        }

        [ConfigurationProperty("PostsPerPage", DefaultValue = 0)]
        public int PostsPerPage
        {
            get { return (int)this["PostsPerPage"]; }
            set { this["PostsPerPage"] = value; }
        }

        [ConfigurationProperty("StartPost", DefaultValue = 1, IsRequired = true)]
        public int StartPost
        {
            get { return (int)this["StartPost"]; }
            set { this["StartPost"] = value; }
        }

        [ConfigurationProperty("EndPost", DefaultValue = 0, IsRequired = true)]
        public int EndPost
        {
            get { return (int)this["EndPost"]; }
            set { this["EndPost"] = value; }
        }

        [ConfigurationProperty("CheckForLastThreadmark", DefaultValue = false)]
        public bool CheckForLastThreadmark
        {
            get { return (bool)this["CheckForLastThreadmark"]; }
            set { this["CheckForLastThreadmark"] = value; }
        }

        [ConfigurationProperty("PartitionMode", DefaultValue = PartitionMode.None)]
        public PartitionMode PartitionMode
        {
            get
            {
                try
                {
                    return (PartitionMode)this["PartitionMode"];
                }
                catch (ConfigurationException)
                {
                    return PartitionMode.None;
                }
            }
            set { this["PartitionMode"] = value; }
        }

        [ConfigurationProperty("UseCustomThreadmarkFilters", DefaultValue = false)]
        public bool UseCustomThreadmarkFilters
        {
            get { return (bool)this["UseCustomThreadmarkFilters"]; }
            set { this["UseCustomThreadmarkFilters"] = value; }
        }

        [ConfigurationProperty("CustomThreadmarkFilters", DefaultValue = "")]
        public string CustomThreadmarkFilters
        {
            get { return (string)this["CustomThreadmarkFilters"]; }
            set { this["CustomThreadmarkFilters"] = value; }
        }

        [ConfigurationProperty("UseCustomUsernameFilters", DefaultValue = false)]
        public bool UseCustomUsernameFilters
        {
            get { return (bool)this["UseCustomUsernameFilters"]; }
            set { this["UseCustomUsernameFilters"] = value; }
        }

        [ConfigurationProperty("CustomUsernameFilters", DefaultValue = "")]
        public string CustomUsernameFilters
        {
            get { return (string)this["CustomUsernameFilters"]; }
            set { this["CustomUsernameFilters"] = value; }
        }

        [ConfigurationProperty("UseCustomPostFilters", DefaultValue = false)]
        public bool UseCustomPostFilters
        {
            get { return (bool)this["UseCustomPostFilters"]; }
            set { this["UseCustomPostFilters"] = value; }
        }

        [ConfigurationProperty("CustomPostFilters", DefaultValue = "")]
        public string CustomPostFilters
        {
            get { return (string)this["CustomPostFilters"]; }
            set { this["CustomPostFilters"] = value; }
        }

        // Obsolete configuration properties.  Left in place solely so that if they
        // exist, they don't cause load errors.  We do not save the results when
        // saving a new config instance.

        [ConfigurationProperty("UseVotePartitions", DefaultValue = false)]
        public bool UseVotePartitions
        {
            get { return (bool)this["UseVotePartitions"]; }
            set { this["UseVotePartitions"] = value; }
        }

        [ConfigurationProperty("PartitionByLine", DefaultValue = true)]
        public bool PartitionByLine
        {
            get { return (bool)this["PartitionByLine"]; }
            set { this["PartitionByLine"] = value; }
        }

        [ConfigurationProperty("AllowRankedVotes", DefaultValue = false)]
        public bool AllowRankedVotes
        {
            get { return (bool)this["AllowRankedVotes"]; }
            set { this["AllowRankedVotes"] = value; }
        }
    }
}
