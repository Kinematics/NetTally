using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public QuestCollection() : base()
        {
            CollectionChanged += QuestCollection_CollectionChanged;
        }

        public QuestCollection(List<IQuest> list) : base(list)
        {
            CollectionChanged += QuestCollection_CollectionChanged;
        }

        public QuestCollection(IEnumerable<IQuest> list) : base(list)
        {
            CollectionChanged += QuestCollection_CollectionChanged;
        }

        public IQuest AddNewQuest()
        {
            var nq = new Quest();
            Add(nq);
            return nq;
        }

        private void QuestCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Prevent duplicates
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                var dupes = from IQuest i in e.NewItems
                            where Items.Count(q => q.Name == i.Name) > 1
                            select i;

                foreach (var dupe in dupes)
                {
                    Remove(dupe);
                }
            }
        }
    }
}
