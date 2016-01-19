using System;
using System.Configuration;

namespace NetTally
{
    /// <summary>
    /// Class to handle the section for storing quests in the user config file.
    /// Also stores the 'current' quest value, and global config options.
    /// </summary>
    public class QuestsSection : ConfigurationSection
    {
        /// <summary>
        /// Defined name of the config section, as saved in the config file.
        /// </summary>
        public const string SectionName = "NetTally.Quests";

        #region Properties
        [ConfigurationProperty("Quests", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(QuestElement))]
        public QuestElementCollection Quests
        {
            get { return (QuestElementCollection)this["Quests"]; }
            set { this["Quests"] = value; }
        }

        [ConfigurationProperty("CurrentQuest", DefaultValue = null)]
        public string CurrentQuest
        {
            get { return (string)this["CurrentQuest"]; }
            set { this["CurrentQuest"] = value; }
        }

        [ConfigurationProperty("AllowRankedVotes", DefaultValue = true)]
        public bool AllowRankedVotes
        {
            get { return (bool)this["AllowRankedVotes"]; }
            set { this["AllowRankedVotes"] = value; }
        }

        [ConfigurationProperty("IgnoreSymbols", DefaultValue = true)]
        public bool IgnoreSymbols
        {
            get { return (bool)this["IgnoreSymbols"]; }
            set { this["IgnoreSymbols"] = value; }
        }

        [ConfigurationProperty("TrimExtendedText", DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this["TrimExtendedText"]; }
            set { this["TrimExtendedText"] = value; }
        }

        [ConfigurationProperty("IgnoreSpoilers", DefaultValue = false)]
        public bool IgnoreSpoilers
        {
            get { return (bool)this["IgnoreSpoilers"]; }
            set { this["IgnoreSpoilers"] = value; }
        }

        [ConfigurationProperty("DisplayMode", DefaultValue = DisplayMode.Normal)]
        public DisplayMode DisplayMode
        {
            get
            {
                try
                {
                    return (DisplayMode)this["DisplayMode"];
                }
                catch (ConfigurationException)
                {
                    return DisplayMode.Normal;
                }
            }
            set { this["DisplayMode"] = value; }
        }
        #endregion

        #region Loading and saving        
        /// <summary>
        /// Loads the configuration information into the provided quest wrapper.
        /// Global information is stored in the advanced options instance.
        /// </summary>
        /// <param name="questWrapper">The quest wrapper.</param>
        public void Load(QuestCollectionWrapper questWrapper)
        {
            AdvancedOptions.Instance.DisplayMode = DisplayMode;
            AdvancedOptions.Instance.AllowRankedVotes = AllowRankedVotes;
            AdvancedOptions.Instance.IgnoreSpoilers = IgnoreSpoilers;
            AdvancedOptions.Instance.IgnoreSymbols = IgnoreSymbols;
            AdvancedOptions.Instance.TrimExtendedText = TrimExtendedText;

            if (questWrapper.QuestCollection == null)
                questWrapper.QuestCollection = new QuestCollection();

            questWrapper.CurrentQuest = CurrentQuest;

            foreach (QuestElement questElement in Quests)
            {
                try
                {
                    IQuest q = new Quest
                    {
                        DisplayName = questElement.DisplayName,
                        ThreadName = questElement.ThreadName,
                        PostsPerPage = questElement.PostsPerPage,
                        StartPost = questElement.StartPost,
                        EndPost = questElement.EndPost,
                        CheckForLastThreadmark = questElement.CheckForLastThreadmark,
                        PartitionMode = questElement.PartitionMode,
                        UseCustomThreadmarkFilters = questElement.UseCustomThreadmarkFilters,
                        CustomThreadmarkFilters = questElement.CustomThreadmarkFilters
                    };

                    if (questElement.UseVotePartitions && q.PartitionMode == PartitionMode.None)
                    {
                        if (questElement.PartitionByLine)
                            q.PartitionMode = PartitionMode.ByLine;
                        else
                            q.PartitionMode = PartitionMode.ByBlock;
                    }

                    questWrapper.QuestCollection.Add(q);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Saves the information from the provided quest wrapper into this config object.
        /// Also pulls global advanced options.
        /// </summary>
        /// <param name="questWrapper">The quest wrapper.</param>
        public void Save(QuestCollectionWrapper questWrapper)
        {
            DisplayMode = AdvancedOptions.Instance.DisplayMode;
            AllowRankedVotes = AdvancedOptions.Instance.AllowRankedVotes;
            IgnoreSymbols = AdvancedOptions.Instance.IgnoreSymbols;
            IgnoreSpoilers = AdvancedOptions.Instance.IgnoreSpoilers;
            TrimExtendedText = AdvancedOptions.Instance.TrimExtendedText;

            CurrentQuest = questWrapper.CurrentQuest;

            Quests.Clear();
            foreach (var quest in questWrapper.QuestCollection)
                Quests.Add(quest);
        }
        #endregion
    }
}
