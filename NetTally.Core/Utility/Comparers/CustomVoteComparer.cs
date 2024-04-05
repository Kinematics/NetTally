using System;
using System.Collections.Generic;
using NetTally.Votes;

namespace NetTally.Comparers
{
    /// <summary>
    /// Custom sorting class for sorting votes.
    /// Sorts by Task+Content.
    /// </summary>
    public class CustomVoteComparer : IComparer<VoteLineBlock>
    {
        public int Compare(object? x, object? y)
        {
            if (x is VoteLineBlock xv && y is VoteLineBlock yv)
            {
                int result = xv.Category.CompareTo(yv.Category);

                if (result == 0)
                    return xv.CompareTo(yv);
                else
                    return result;
            }
            else
            {
                throw new ArgumentException("Parameters are not known types.");
            }
        }

        public int Compare(VoteLineBlock? x, VoteLineBlock? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            int categoryComparison = x.Category.CompareTo(y.Category);

            if (categoryComparison == 0)
                return x.CompareTo(y);
            else
                return categoryComparison;
        }
    }
}
