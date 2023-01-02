using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NetTally.Global
{
    /// <summary>
    /// Quest info interface that allows modifying quest info content.
    /// </summary>
    public interface IQuestsInfoMod
    {
        /// <summary>
        /// Gets an observable collection of quests.
        /// </summary>
        ObservableCollection<Quest> Quests { get; }

        /// <summary>
        /// Gets or sets currently selected quest.
        /// </summary>
        Quest? SelectedQuest { get; set; }

        /// <summary>
        /// Get a list of any linked quests associated with the provided quest.
        /// </summary>
        /// <param name="quest">The quest to get linked quests for.</param>
        /// <returns>Returns a list of any linked quests.</returns>
        List<Quest> GetLinkedQuests(Quest quest);

        /// <summary>
        /// Create a new quest. Ensures the quest has a vote counter and
        /// has been saved in the Quests collection.
        /// If a NewThreadEntry quest already exists, return that instead.
        /// </summary>
        /// <returns>Returns a new quest.</returns>
        Quest CreateQuest();

        /// <summary>
        /// Remove the selected quest.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was removed.</returns>
        bool RemoveQuest(Quest quest);
    }
}
