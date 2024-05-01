using System.Collections;
using System.Collections.Generic;
using NetTally.Tally.Components;

namespace NetTally.Comparers
{
    /// <summary>
    /// Custom sorting class for sorting votes.
    /// Sorts by Task+Content.
    /// </summary>
    public class VoteBlockComparer : IComparer<VoteLineBlock>, IComparer
    {
        public int Compare(object? x, object? y)
        {
            return Compare(x as VoteLineBlock, y as VoteLineBlock);
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
