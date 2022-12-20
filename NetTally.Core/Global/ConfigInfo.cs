using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NetTally.Global
{
    public class ConfigInfoWrapper
    {
        public required ConfigInfo ConfigInfo { get; init; }
    }

    public class ConfigInfo
    {
        public ConfigInfo() 
        {
        }

        [SetsRequiredMembers]
        public ConfigInfo(IEnumerable<Quest> quests, GlobalSettings globalSettings)
        {
            UserQuests = new(quests);
            GlobalSettings = globalSettings;
        }

        public required GlobalSettings GlobalSettings { get; init; }

        public required UserQuests UserQuests { get; init; }
    }
}
