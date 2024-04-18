using System.Collections.Generic;

namespace NetTally.Configure
{
    public class ConfigInfo
    {
        public ConfigInfo()
        {
            UserQuests = new();
            GlobalSettings = new();
        }

        public ConfigInfo(IEnumerable<Quest> quests, string? currentQuest, GlobalSettings globalSettings)
        {
            UserQuests = new(quests, currentQuest);
            GlobalSettings = globalSettings;
        }

        public GlobalSettings GlobalSettings { get; init; }

        public UserQuests UserQuests { get; init; }
    }
}
