using System;
using System.Collections;
using NetTally.Votes;

namespace NetTally.Comparers
{
    /// <summary>
    /// Custom sorting class for sorting votes.
    /// Sorts by Task+Content.
    /// </summary>
    public class CustomVoteSort : IComparer
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
    }
}
