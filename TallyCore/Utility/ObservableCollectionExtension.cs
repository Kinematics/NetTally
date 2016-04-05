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
    public class ObservableCollectionExt<T> : ObservableCollection<T>
    {
        public ObservableCollectionExt()
        {
        }

        public ObservableCollectionExt(IEnumerable<T> list)
            : base(list)
        {
        }

        /// <summary>
        /// Removes all matching instances from the collection before notifying
        /// about changes.
        /// </summary>
        /// <param name="predicate">The predicate indicating what to remove.</param>
        public void RemoveWhere(Predicate<T> predicate)
        {
            CheckReentrancy();

            List<T> itemsToRemove = Items.Where(x => predicate(x)).ToList();
            foreach (var item in itemsToRemove)
                Items.Remove(item);

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Adds a range of values to the collection before notifying about changes.
        /// </summary>
        /// <param name="list">The list of items to add.</param>
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

        /// <summary>
        /// Replaces the current collection with the contents of the specified list.
        /// </summary>
        /// <param name="list">The list of new items for the collection.</param>
        public void Replace(IEnumerable<T> list)
        {
            CheckReentrancy();

            Items.Clear();

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
