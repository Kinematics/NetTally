using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NetTally.Global
{
    /// <summary>
    /// Read-only interface to quest info content.
    /// </summary>
    public interface IQuestsInfo
    {
        /// <summary>
        /// Gets an observable collection of quests.
        /// </summary>
        ObservableCollection<Quest> Quests { get; }

        /// <summary>
        /// Gets the currently selected quest.
        /// </summary>
        Quest? SelectedQuest { get; }

        /// <summary>
        /// Get a list of any linked quests associated with the provided quest.
        /// </summary>
        /// <param name="quest">The quest to get linked quests for.</param>
        /// <returns>Returns a list of any linked quests.</returns>
        List<Quest> GetLinkedQuests(Quest quest);
    }
}
