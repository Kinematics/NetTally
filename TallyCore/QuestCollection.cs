using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace NetTally
{
    /// <summary>
    /// Generic observable collection, that can be used in place of the custom
    /// collection that was manually implemented.
    /// </summary>
    [CollectionDataContract(ItemName ="Quest")]
    [KnownType(typeof(Quest))]
    public class QuestCollection : ObservableCollection<IQuest>
    {
        public IQuest AddNewQuest()
        {
            var nq = new Quest();
            Add(nq);
            if (this.Contains(nq))
                return nq;
            else
                return null;
        }

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
