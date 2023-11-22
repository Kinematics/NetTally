using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NetTally.Collections;
using NetTally.Options;
using NetTally.Types.Enums;

namespace NetTally.Avalonia.Config
{
    /// <summary>
    /// Class to handle the section for storing quests in the user config file.
    /// Also stores the 'current' quest value, and global config options.
    /// </summary>
    public class QuestsSection : ConfigurationSection
    {
        #region Meta info
        /// <summary>
        /// A list of all deprecated attributes.
        /// </summary>
        readonly string[] deprecatedAttributes = ["IgnoreSymbols", "AllowVoteLabelPlanNames"];

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
        [ConfigurationProperty(nameof(Quests), IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(QuestElement))]
        public QuestElementCollection Quests
        {
            get { return (QuestElementCollection)this[nameof(Quests)]; }
            set { this[nameof(Quests)] = value; }
        }

        [ConfigurationProperty(nameof(CurrentQuest), DefaultValue = null)]
        public string CurrentQuest
        {
            get { return (string)this[nameof(CurrentQuest)]; }
            set { this[nameof(CurrentQuest)] = value; }
        }

        [ConfigurationProperty(nameof(AllowRankedVotes), DefaultValue = true)]
        public bool AllowRankedVotes
        {
            get { return (bool)this[nameof(AllowRankedVotes)]; }
            set { this[nameof(AllowRankedVotes)] = value; }
        }

        [ConfigurationProperty(nameof(GlobalSpoilers), DefaultValue = false)]
        public bool GlobalSpoilers
        {
            get { return (bool)this[nameof(GlobalSpoilers)]; }
            set { this[nameof(GlobalSpoilers)] = value; }
        }

        [ConfigurationProperty(nameof(DisplayPlansWithNoVotes), DefaultValue = false)]
        public bool DisplayPlansWithNoVotes
        {
            get { return (bool)this[nameof(DisplayPlansWithNoVotes)]; }
            set { this[nameof(DisplayPlansWithNoVotes)] = value; }
        }

        [ConfigurationProperty(nameof(DisplayMode), DefaultValue = DisplayMode.Normal)]
        public DisplayMode DisplayMode
        {
            get
            {
                try
                {
                    return (DisplayMode)this[nameof(DisplayMode)];
                }
                catch (ConfigurationException)
                {
                    return DisplayMode.Normal;
                }
            }
            set { this[nameof(DisplayMode)] = value; }
        }

        [ConfigurationProperty(nameof(DisableWebProxy), DefaultValue = false)]
        public bool DisableWebProxy
        {
            get { return (bool)this[nameof(DisableWebProxy)]; }
            set { this[nameof(DisableWebProxy)] = value; }
        }

        [ConfigurationProperty(nameof(AllowUsersToUpdatePlans), DefaultValue = BoolEx.Unknown)]
        public BoolEx AllowUsersToUpdatePlans
        {
            get
            {
                try
                {
                    return (BoolEx)this[nameof(AllowUsersToUpdatePlans)];
                }
                catch (ConfigurationException)
                {
                    return BoolEx.Unknown;
                }
            }
            set { this[nameof(AllowUsersToUpdatePlans)] = value; }
        }

        #endregion

        #region Obsolete Properties
#pragma warning disable CS0618 // Type or member is obsolete

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(WhitespaceAndPunctuationIsSignificant), DefaultValue = false)]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return (bool)this[nameof(WhitespaceAndPunctuationIsSignificant)]; }
            set { this[nameof(WhitespaceAndPunctuationIsSignificant)] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(ForbidVoteLabelPlanNames), DefaultValue = false)]
        public bool ForbidVoteLabelPlanNames
        {
            get { return (bool)this[nameof(ForbidVoteLabelPlanNames)]; }
            set { this[nameof(ForbidVoteLabelPlanNames)] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(DisableProxyVotes), DefaultValue = false)]
        public bool DisableProxyVotes
        {
            get { return (bool)this[nameof(DisableProxyVotes)]; }
            set { this[nameof(DisableProxyVotes)] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(ForcePinnedProxyVotes), DefaultValue = false)]
        public bool ForcePinnedProxyVotes
        {
            get { return (bool)this[nameof(ForcePinnedProxyVotes)]; }
            set { this[nameof(ForcePinnedProxyVotes)] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(TrimExtendedText), DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this[nameof(TrimExtendedText)]; }
            set { this[nameof(TrimExtendedText)] = value; }
        }

        [Obsolete("Moved to QuestElement")]
        [ConfigurationProperty(nameof(IgnoreSpoilers), DefaultValue = false)]
        public bool IgnoreSpoilers
        {
            get { return (bool)this[nameof(IgnoreSpoilers)]; }
            set { this[nameof(IgnoreSpoilers)] = value; }
        }

#pragma warning restore CS0618 // Type or member is obsolete
        #endregion Obsolete Properties

        #region Loading and saving        
        /// <summary>
        /// Loads the configuration information into the provided quest wrapper.
        /// Global information is stored in the advanced options instance.
        /// </summary>
        /// <param name="quests">The collection of saved quests.</param>
        /// <param name="currentQuest">The currently selected quest.</param>
        /// <param name="options">The program configuration options.</param>
        public void Load(out QuestCollection quests, out string currentQuest, AdvancedOptions? options)
        {
            currentQuest = CurrentQuest;
            quests = [];
            Dictionary<Quest, string> linkedQuestNames = [];

            foreach (QuestElement? questElement in Quests)
            {
                if (questElement == null)
                    continue;

                try
                {
                    Quest q = new()
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
                        CaseIsSignificant = questElement.CaseIsSignificant,
                        ForcePlanReferencesToBeLabeled = questElement.ForcePlanReferencesToBeLabeled,
                        ForbidVoteLabelPlanNames = questElement.ForbidVoteLabelPlanNames,
                        AllowUsersToUpdatePlans = questElement.AllowUsersToUpdatePlans,
                        DisableProxyVotes = questElement.DisableProxyVotes,
                        ForcePinnedProxyVotes = questElement.ForcePinnedProxyVotes,
                        IgnoreSpoilers = questElement.IgnoreSpoilers,
                        TrimExtendedText = questElement.TrimExtendedText,
                        UseRSSThreadmarks = questElement.UseRSSThreadmarks,
                    };

                    if (!string.IsNullOrEmpty(questElement.LinkedQuests))
                        linkedQuestNames[q] = questElement.LinkedQuests;

                    quests.Add(q);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            //foreach (var (quest, linkedNames) in linkedQuestNames)
            //{
            //    var names = linkedNames.Split(new char[] { '⦂' });

            //    foreach (var name in names)
            //    {
            //        var linkedQuest = quests.FirstOrDefault(q => q.ThreadName == name);

            //        if (linkedQuest != null)
            //        {
            //            quest.LinkedQuests.Add(linkedQuest);
            //        }
            //    }
            //}

            if (options != null)
            {
                options.DisplayMode = DisplayMode;
                options.AllowUsersToUpdatePlans = AllowUsersToUpdatePlans;
                options.GlobalSpoilers = GlobalSpoilers;
                options.DisplayPlansWithNoVotes = DisplayPlansWithNoVotes;
                options.DisableWebProxy = DisableWebProxy;
            }
        }
        #endregion
    }
}
