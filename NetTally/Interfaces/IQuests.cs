using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public interface IQuests
    {
        /// <summary>
        /// Get a list of the names of the quests currently in the list.
        /// </summary>
        List<string> QuestListNames { get; }
        /// <summary>
        /// Gets and sets the current quest.
        /// </summary>
        IQuest CurrentQuest { get; set; }

        /// <summary>
        /// Add the specified quest.
        /// </summary>
        /// <param name="quest">The quest to add.</param>
        /// <returns>Returns true if it was successfully added to the list.</returns>
        bool AddQuest(IQuest quest);
        /// <summary>
        /// Add a new quest (type Quest).
        /// </summary>
        /// <returns>Returns the new quest if it was added, or the existing quest of the same name
        /// if the new quest could not be added.</returns>
        IQuest AddNewQuest();
        /// <summary>
        /// Remove the specified quest.
        /// </summary>
        /// <param name="quest">The quest to remove.</param>
        /// <returns>Returns true if the quest was found and removed.</returns>
        bool RemoveQuest(IQuest quest);
        /// <summary>
        /// Remove the current quest.
        /// </summary>
        /// <returns>Returns the current quest object that was removed.</returns>
        bool RemoveCurrentQuest();
        /// <summary>
        /// Clear the list of quests.
        /// </summary>
        void Clear();


        /// <summary>
        /// Get a quest by name.
        /// </summary>
        /// <param name="name">The name of the quest to get.</param>
        /// <returns>Returns the quest, if found.</returns>
        IQuest GetQuestByName(string name);
        /// <summary>
        /// Sets the current quest to the quest specified by name.
        /// </summary>
        /// <param name="name">The name of the quest to be made current.</param>
        void SetCurrentQuestByName(string name);


    }
}
