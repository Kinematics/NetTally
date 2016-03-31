using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace NetTally.Utility
{
    /// <summary>
    /// An extension of the ObservableCollection class that allows specialized
    /// adding and removing functions that only generate a property changed
    /// notification after all adds/removes are complete, rather than after
    /// each one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Collections.ObjectModel.ObservableCollection{T}" />
    [Serializable]
    public class ObservableCollectionExt<T> : ObservableCollection<T>
    {
        public ObservableCollectionExt()
        {
        }

        public ObservableCollectionExt(IEnumerable<T> list)
            : base(list)
        {
        }

        public ObservableCollectionExt(List<T> list)
            : base(list)
        {
        }

        public void RemoveWhere(Predicate<T> predicate)
        {
            CheckReentrancy();

            List<T> itemsToRemove = Items.Where(x => predicate(x)).ToList();
            itemsToRemove.ForEach(item => Items.Remove(item));

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<T> list)
        {
            CheckReentrancy();

            foreach (T item in list)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
