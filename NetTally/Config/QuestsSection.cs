using System;
using System.Configuration;
using System.Linq;
using NetTally.Collections;
using NetTally.Output;
using NetTally.Votes;

namespace NetTally
{
    /// <summary>
    /// Class to handle the section for storing quests in the user config file.
    /// Also stores the 'current' quest value, and global config options.
    /// </summary>
    public class QuestsSection : ConfigurationSection
    {
        #region Meta info
        /// <summary>
        /// Defined name of the config section, as saved in the config file.
        /// </summary>
        public const string SectionName = "NetTally.Quests";

        /// <summary>
        /// A list of all deprecated attributes.
        /// </summary>
        string[] deprecatedAttributes = new string[] { "IgnoreSymbols", "AllowVoteLabelPlanNames" };

        /// <summary>
        /// Gets a value indicating whether an unknown attribute is encountered during deserialization.
        /// Strict mode will throw an exception on unknown, non-deprecated attributes.
        /// Non-strict mode will ignore all unknown attributes.
        /// </summary>
        /// <param name="name">The name of the unrecognized attribute.</param>
        /// <param name="value">The value of the unrecognized attribute.</param>
        /// <returns>
        /// true when an unknown attribute is encountered while deserializing, and we don't want to throw
        /// an exception. Otherwise, false.
        /// </returns>
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

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("WhitespaceAndPunctuationIsSignificant", DefaultValue = false)]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return (bool)this["WhitespaceAndPunctuationIsSignificant"]; }
            set { this["WhitespaceAndPunctuationIsSignificant"] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("ForbidVoteLabelPlanNames", DefaultValue = false)]
        public bool ForbidVoteLabelPlanNames
        {
            get { return (bool)this["ForbidVoteLabelPlanNames"]; }
            set { this["ForbidVoteLabelPlanNames"] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("DisableProxyVotes", DefaultValue = false)]
        public bool DisableProxyVotes
        {
            get { return (bool)this["DisableProxyVotes"]; }
            set { this["DisableProxyVotes"] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("ForcePinnedProxyVotes", DefaultValue = false)]
        public bool ForcePinnedProxyVotes
        {
            get { return (bool)this["ForcePinnedProxyVotes"]; }
            set { this["ForcePinnedProxyVotes"] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("TrimExtendedText", DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this["TrimExtendedText"]; }
            set { this["TrimExtendedText"] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty("IgnoreSpoilers", DefaultValue = false)]
        public bool IgnoreSpoilers
        {
            get { return (bool)this["IgnoreSpoilers"]; }
            set { this["IgnoreSpoilers"] = value; }
        }

        [ConfigurationProperty("GlobalSpoilers", DefaultValue = false)]
        public bool GlobalSpoilers
        {
            get { return (bool)this["GlobalSpoilers"]; }
            set { this["GlobalSpoilers"] = value; }
        }

        [ConfigurationProperty("DisplayPlansWithNoVotes", DefaultValue = false)]
        public bool DisplayPlansWithNoVotes
        {
            get { return (bool)this["DisplayPlansWithNoVotes"]; }
            set { this["DisplayPlansWithNoVotes"] = value; }
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

        [ConfigurationProperty("DisableWebProxy", DefaultValue = false)]
        public bool DisableWebProxy
        {
            get { return (bool)this["DisableWebProxy"]; }
            set { this["DisableWebProxy"] = value; }
        }

        #endregion

        #region Loading and saving        
        /// <summary>
        /// Loads the configuration information into the provided quest wrapper.
        /// Global information is stored in the advanced options instance.
        /// </summary>
        /// <param name="quests">The collection of saved quests.</param>
        /// <param name="currentQuest">The currently selected quest.</param>
        /// <param name="options">The program configuration options.</param>
        public void Load(out QuestCollection quests, out string currentQuest, AdvancedOptions options)
        {
            currentQuest = CurrentQuest;
            quests = new QuestCollection();

            foreach (QuestElement questElement in Quests)
            {
                try
                {
                    Quest q = new Quest
                    {
                        DisplayName = questElement.DisplayName,
                        ThreadName = questElement.ThreadName,
                        PostsPerPage = questElement.PostsPerPage,
                        StartPost = questElement.StartPost,
                        EndPost = questElement.EndPost,
                        CheckForLastThreadmark = questElement.CheckForLastThreadmark,
                        PartitionMode = questElement.PartitionMode,
                        UseCustomThreadmarkFilters = questElement.UseCustomThreadmarkFilters,
                        CustomThreadmarkFilters = questElement.CustomThreadmarkFilters,
                        UseCustomUsernameFilters = questElement.UseCustomUsernameFilters,
                        CustomUsernameFilters = questElement.CustomUsernameFilters,
                        UseCustomPostFilters = questElement.UseCustomPostFilters,
                        CustomPostFilters = questElement.CustomPostFilters,
                        WhitespaceAndPunctuationIsSignificant = questElement.WhitespaceAndPunctuationIsSignificant,
                        ForbidVoteLabelPlanNames = questElement.ForbidVoteLabelPlanNames,
                        DisableProxyVotes = questElement.DisableProxyVotes,
                        ForcePinnedProxyVotes = questElement.ForcePinnedProxyVotes,
                        IgnoreSpoilers = questElement.IgnoreSpoilers,
                        TrimExtendedText = questElement.TrimExtendedText,
                        UseRSSThreadmarks = questElement.UseRSSThreadmarks,
                    };

                    quests.Add(q);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (options != null)
            {
                options.DisplayMode = DisplayMode;
                options.AllowRankedVotes = AllowRankedVotes;
                options.GlobalSpoilers = GlobalSpoilers;
                options.DisplayPlansWithNoVotes = DisplayPlansWithNoVotes;
                options.DisableWebProxy = DisableWebProxy;
            }
        }

        /// <summary>
        /// Saves the information from the provided quest wrapper into this config object.
        /// Also pulls global advanced options.
        /// </summary>
        /// <param name="quests">The collection of saved quests.</param>
        /// <param name="currentQuest">The currently selected quest.</param>
        /// <param name="options">The program configuration options.</param>
        public void Save(QuestCollection quests, string currentQuest, AdvancedOptions options)
        {
            if (quests == null)
                throw new ArgumentNullException(nameof(quests));

            Quests.Clear();
            foreach (var quest in quests)
                Quests.Add(quest);

            CurrentQuest = currentQuest;

            if (options != null)
            {
                DisplayMode = options.DisplayMode;
                AllowRankedVotes = options.AllowRankedVotes;
                GlobalSpoilers = options.GlobalSpoilers;
                DisplayPlansWithNoVotes = options.DisplayPlansWithNoVotes;
                DisableWebProxy = options.DisableWebProxy;
            }
        }
        #endregion
    }
}
