using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NetTally.Global
{
    public class ConfigInfo
    {
        [SetsRequiredMembers]
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

        public required GlobalSettings GlobalSettings { get; init; }

        public required UserQuests UserQuests { get; init; }
    }
}
