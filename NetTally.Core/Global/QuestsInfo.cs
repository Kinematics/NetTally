using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTally.VoteCounting;

namespace NetTally.Global
{
    public partial class QuestsInfo
    {
        public QuestsInfo(
            IOptions<GlobalSettings> globalSettings,
            IOptions<UserQuests> userQuests,
            ConfigInfo legacyConfig,
            IServiceProvider serviceProvider)
        {
            if (userQuests.Value.Quests.Count == 0 &&
                legacyConfig.UserQuests.Quests.Count > 0)
            {
                Quests = new ObservableCollection<Quest>(legacyConfig.UserQuests.Quests);

                if (!string.IsNullOrEmpty(legacyConfig.UserQuests.CurrentQuest))
                {
                    SelectedQuest = legacyConfig.UserQuests.Quests.FirstOrDefault(q => q.ThreadName == legacyConfig.UserQuests.CurrentQuest);
                }

                globalSettings.Value.UpdateFromLegacySettings(legacyConfig.GlobalSettings);
            }
            else
            {
                Quests = new ObservableCollection<Quest>(userQuests.Value.Quests);

                if (!string.IsNullOrEmpty(userQuests.Value.CurrentQuest))
                {
                    SelectedQuest = userQuests.Value.Quests.FirstOrDefault(q => q.ThreadName == userQuests.Value.CurrentQuest);
                }
            }

            InjectVoteCounters(serviceProvider);
        }

        private void InjectVoteCounters(IServiceProvider serviceProvider)
        {
            foreach (var quest in Quests)
            {
                var voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
                quest.VoteCounter = voteCounter;
            }
        }

        public ObservableCollection<Quest> Quests { get; } = new();

        public Quest? SelectedQuest { get; set; }

    }
}
