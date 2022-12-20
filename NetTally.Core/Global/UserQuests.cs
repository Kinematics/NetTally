using System.Collections.Generic;

namespace NetTally.Global
{
    public class UserQuests
    {
        public UserQuests() { }

        public UserQuests(IEnumerable<Quest> quests, string? currentQuest)
        {
            Quests.AddRange(quests);
            CurrentQuest = currentQuest ?? "";
        }

        public string CurrentQuest { get; set; } = string.Empty;

        public List<Quest> Quests { get; set; } = new();
    }
}
