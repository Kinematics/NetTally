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
        const string defaultSite = "http://forums.sufficientvelocity.com/";

        /// <summary>
        /// Indexer into the collection by quest name.
        /// </summary>
        /// <param name="name">The name of the quest to look for.</param>
        /// <returns>Returns the quest if found, or null if not.</returns>
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
            var dupes = from q in this
                        where q.Name == item.Name && 
                            (q.Site == item.Site || (q.Site == string.Empty && item.Site == defaultSite) || (q.Site == defaultSite && item.Site == string.Empty))
                        select q;

            if (dupes.Any())
                return;

            base.InsertItem(index, item);
        }
    }
}
