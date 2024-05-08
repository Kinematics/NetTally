using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Data;
using NetTally.VoteCounting;

namespace NetTally.Configure
{
    /// <summary>
    /// Class to store the list of quests a user has added, as well
    /// as the currently selected quest.
    /// </summary>
    public partial class QuestsInfo : IQuestsInfo, IQuestsInfoMod
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<QuestsInfo> logger;

        public QuestsInfo(
            IOptions<GlobalSettings> globalSettings,
            IOptions<UserQuests> userQuests,
            ConfigInfo legacyConfig,
            IServiceProvider serviceProvider,
            ILogger<QuestsInfo> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;

            // If there are no user quests, but there are legacy quests,
            // load the legacy information. Otherwise load user information.
            if (userQuests.Value.Quests.Count == 0 &&
                legacyConfig.UserQuests.Quests.Count > 0)
            {
                LoadLegacyQuests(legacyConfig, globalSettings.Value);
            }
            else
            {
                LoadUserQuests(userQuests.Value);
            }

            InjectVoteCounters();
        }

        /// <summary>
        /// Load legacy quests from legacy config information.
        /// Also update global settings from legacy config.
        /// </summary>
        /// <param name="legacyConfig">Legacy XML config information.</param>
        /// <param name="globalSettings">Global settings that need to be updated with legacy info.</param>
        private void LoadLegacyQuests(ConfigInfo legacyConfig, GlobalSettings globalSettings)
        {
            Quests = new ObservableCollection<Quest>(legacyConfig.UserQuests.Quests);

            if (!string.IsNullOrEmpty(legacyConfig.UserQuests.CurrentQuest))
            {
                SelectedQuest = legacyConfig.UserQuests.Quests.FirstOrDefault(q => q.ThreadName == legacyConfig.UserQuests.CurrentQuest);
            }

            globalSettings.UpdateFromLegacySettings(legacyConfig.GlobalSettings);

            logger.LogDebug("Loaded {count} legacy quests", Quests.Count);
        }

        /// <summary>
        /// Load user quests from config information.
        /// </summary>
        /// <param name="userQuests"></param>
        private void LoadUserQuests(UserQuests userQuests)
        {
            Quests = new ObservableCollection<Quest>(userQuests.Quests);

            if (Quests.Count > 0 && !string.IsNullOrEmpty(userQuests.CurrentQuest))
            {
                SelectedQuest = userQuests.Quests.FirstOrDefault(q => q.ThreadName == userQuests.CurrentQuest);
            }

            logger.LogDebug("Loaded {count} user quests", Quests.Count);
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
        public ObservableCollection<Quest> Quests { get; private set; } = [];

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
            if (Quests.FirstOrDefault(q => q.ThreadName == StringData.NewThreadEntry) is not Quest quest)
            {
                quest = new Quest
                {
                    VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>(),
                    CheckForLastThreadmark = true
                };

                Quests.Add(quest);
            }

            return quest;
        }

        /// <summary>
        /// Reposition the specified quest in alphabetical order.
        /// </summary>
        /// <param name="quest">The quest to reposition in the Quests collection.</param>
        public void RepositionQuest(Quest? quest)
        {
            if (quest is null)
                return;

            var index = Quests.IndexOf(quest);

            if (index == -1)
                return;

            int newIndex = 0;
            for (; newIndex < Quests.Count; newIndex++)
            {
                if (Quests[newIndex].DisplayName.CompareTo(quest.DisplayName) > 0)
                    break;
            }

            if (newIndex >= Quests.Count)
                newIndex = Quests.Count - 1;
            
            if (index == newIndex)
                return;

            Quests.Move(index, newIndex);

            logger.LogDebug("Moved quest {name} from position {start} to position {end}.", quest.DisplayName, index, newIndex);
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

            logger.LogDebug("Removing quest {name}", quest.DisplayName);

            return Quests.Remove(quest);
        }

        /// <summary>
        /// Get a list of any linked quests associated with the provided quest.
        /// </summary>
        /// <param name="quest">The quest to get linked quests for.</param>
        /// <returns>Returns a list of any linked quests.</returns>
        public List<Quest> GetLinkedQuests(Quest quest)
        {
            return Quests.Where(quest.HasLinkedQuest).ToList();
        }
    }
}
