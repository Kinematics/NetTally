using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

namespace NetTally.Utility
{
    /// <summary>
    /// Custom sorting class for sorting votes.
    /// Sorts by Task+Content.
    /// </summary>
    public class CustomVoteSort : IComparer
    {
        public int Compare(object x, object y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));
            if (y == null)
                throw new ArgumentNullException(nameof(y));

            string xs = x as string;
            if (xs == null)
                throw new ArgumentException("Parameter x is not a string.");

            string ys = y as string;
            if (ys == null)
                throw new ArgumentException("Parameter x is not a string.");

            string marker = VoteString.GetVoteMarker(xs);
            VoteType voteType = string.IsNullOrEmpty(marker) ? VoteType.Rank : VoteType.Plan;

            string compX = VoteString.GetVoteTask(xs, voteType) + " " + VoteString.GetVoteContent(xs, voteType);
            string compY = VoteString.GetVoteTask(ys, voteType) + " " + VoteString.GetVoteContent(ys, voteType);

            int result = string.Compare(compX, compY, CultureInfo.CurrentUICulture, CompareOptions.IgnoreCase);

            return result;
        }
    }



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
