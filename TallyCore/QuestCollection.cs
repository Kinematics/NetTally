using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace NetTally
{
    /// <summary>
    /// Generic observable collection for Quest items.
    /// Can be serialized via Data Contract.
    /// </summary>
    [CollectionDataContract(ItemName ="Quest")]
    [KnownType(typeof(Quest))]
    public class QuestCollection : ObservableCollection<IQuest>
    {
        public IQuest this[string name] => this.FirstOrDefault(q => q.Name == name);

        /// <summary>
        /// Add a new quest to the current collection.
        /// </summary>
        /// <returns>Returns the newly created quest if it was successfully added,
        /// or returns null if it was not (ie: duplicate).</returns>
        public IQuest AddNewQuest()
        {
            var nq = new Quest();
            Add(nq);
            if (this.Contains(nq))
                return nq;
            else
                return null;
        }

        /// <summary>
        /// Override InsertItem so that we can prevent duplicate entries.
        /// </summary>
        /// <param name="index">Index to enter the new item at.</param>
        /// <param name="item">Item to be entered.</param>
        protected override void InsertItem(int index, IQuest item)
        {
            if (this.Any(q => q.Name == item.Name))
            {
                Debug.WriteLine("Attempting to add duplicate value of name: " + item.Name);
                return;
            }

            base.InsertItem(index, item);
        }
    }
}
