using System.Collections.Generic;

namespace NetTally.Global
{
    public class UserQuests
    {
        public UserQuests() { }

        public UserQuests(IEnumerable<Quest> quests)
        {
            Quests.AddRange(quests);
        }

        public List<Quest> Quests { get; set; } = new();
    }
}
