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
    }
}
