﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace NetTally.Collections
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
        private SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

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
            else if (_synchronizationContext is not null)
            {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object? param)
        {
            // We are in the creator thread, call the base implementation directly
            if (param is NotifyCollectionChangedEventArgs e)
            {
                base.OnCollectionChanged(e);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else if (_synchronizationContext is not null)
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object? param)
        {
            // We are in the creator thread, call the base implementation directly
            if (param is PropertyChangedEventArgs e)
            {
                base.OnPropertyChanged(e);
            }
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

            if (Items != null)
            {
                //var removedItems = Items.Where(i => predicate(i)).ToList();

                var itemsBeingKept = Items.Where(a => !predicate(a)).ToList();

                Items.Clear();

                if (Items is List<T> itemsList)
                {
                    itemsList.AddRange(itemsBeingKept);
                }
                else
                {
                    foreach (var item in itemsBeingKept)
                        Items.Add(item);
                }

                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                //NotifyCollectionChangedEventArgs does not support multiple removed items.
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
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

            //var addedItems = list.ToList();

            foreach (T item in list)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            //NotifyCollectionChangedEventArgs does not support multiple added items.
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems));
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

            var originalItems = Items.ToList();

            Items.Clear();

            foreach (T item in list)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            //NotifyCollectionChangedEventArgs does not support multiple replaced items.
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
            //    Items.ToList(), originalItems));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Sorts the current collection.
        /// </summary>
        public void Sort()
        {
            CheckReentrancy();

            var originalItems = Items.ToList();

            if (Items is List<T> itemsList)
            {
                itemsList.Sort();
            }
            else if (Items != null)
            {
                List<T> list = new List<T>(Items);
                list.Sort();

                Items.Clear();
                foreach (T item in list)
                {
                    Items.Add(item);
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            //NotifyCollectionChangedEventArgs does not support multiple replaced items.
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
            //    Items.ToList(), originalItems));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        #endregion
    }
}
