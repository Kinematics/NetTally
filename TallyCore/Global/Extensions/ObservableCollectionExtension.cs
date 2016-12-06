using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace NetTally.Extensions
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
        #region Constructor
        private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        public ObservableCollectionExt()
        {
        }

        public ObservableCollectionExt(IEnumerable<T> list)
            : base(list)
        {
        }
        #endregion

        #region Handling raising events on proper synchronization context.
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the CollectionChanged event on the current thread
                RaiseCollectionChanged(e);
            }
            else
            {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
        }
        #endregion

        #region Custom modification functions
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
            if (list == null)
                return;

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
            if (list == null)
                return;

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
        #endregion
    }
}
