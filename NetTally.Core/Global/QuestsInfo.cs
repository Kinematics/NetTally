using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.VoteCounting;

namespace NetTally.Global
{
    /// <summary>
    /// Class to store the list of quests a user has added, as well
    /// as the currently selected quest.
    /// </summary>
    public partial class QuestsInfo : IQuestsInfo, IQuestsInfoMod
    {
        private readonly IServiceProvider serviceProvider;

        public QuestsInfo(
            IOptions<GlobalSettings> globalSettings,
            IOptions<UserQuests> userQuests,
            ConfigInfo legacyConfig,
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            // If there are no user quests, but there are legacy quests,
            // load the legacy information. Otherwise load user information.
            if (userQuests.Value.Quests.Count == 0 &&
                legacyConfig.UserQuests.Quests.Count > 0)
            {
                LoadLegacyQuests(globalSettings, legacyConfig);
            }
            else
            {
                LoadUserQuests(userQuests);
            }

            InjectVoteCounters();
        }

        /// <summary>
        /// Load legacy quests from legacy config information.
        /// Also update global settings from legacy config.
        /// </summary>
        /// <param name="globalSettings"></param>
        /// <param name="legacyConfig"></param>
        private void LoadLegacyQuests(IOptions<GlobalSettings> globalSettings, ConfigInfo legacyConfig)
        {
            Quests = new ObservableCollection<Quest>(legacyConfig.UserQuests.Quests);

            if (!string.IsNullOrEmpty(legacyConfig.UserQuests.CurrentQuest))
            {
                SelectedQuest = legacyConfig.UserQuests.Quests.FirstOrDefault(q => q.ThreadName == legacyConfig.UserQuests.CurrentQuest);
            }

            globalSettings.Value.UpdateFromLegacySettings(legacyConfig.GlobalSettings);
        }

        /// <summary>
        /// Load user quests from config information.
        /// </summary>
        /// <param name="userQuests"></param>
        private void LoadUserQuests(IOptions<UserQuests> userQuests)
        {
            Quests = new ObservableCollection<Quest>(userQuests.Value.Quests);

            if (!string.IsNullOrEmpty(userQuests.Value.CurrentQuest))
            {
                SelectedQuest = userQuests.Value.Quests.FirstOrDefault(q => q.ThreadName == userQuests.Value.CurrentQuest);
            }
        }

        /// <summary>
        /// Ensure all quests are provided their own instance of a vote counter.
        /// </summary>
        private void InjectVoteCounters()
        {
            foreach (var quest in Quests)
            {
                quest.VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            }
        }

        /// <summary>
        /// Gets an observable collection of quests.
        /// </summary>
        public ObservableCollection<Quest> Quests { get; private set; } = new();

        /// <summary>
        /// Gets or sets currently selected quest.
        /// </summary>
        public Quest? SelectedQuest { get; set; }

        /// <summary>
        /// Create a new quest. Ensures the quest has a vote counter and
        /// has been saved in the Quests collection.
        /// If a NewThreadEntry quest already exists, return that instead.
        /// </summary>
        /// <returns>Returns a new quest.</returns>
        public Quest CreateQuest()
        {
            if (Quests.FirstOrDefault(q => q.ThreadName == Quest.NewThreadEntry) is not Quest quest)
            {
                quest = new Quest
                {
                    VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>()
                };

                Quests.Add(quest);
            }

            return quest;
        }

        /// <summary>
        /// Remove the selected quest.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was removed.</returns>
        public bool RemoveQuest(Quest quest)
        {
            if (quest is null)
                return false;

            if (quest == SelectedQuest)
                SelectedQuest = null;

            return Quests.Remove(quest);
        }
    }
}
