using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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

            string compX = VoteString.GetVoteTask(xs) + " " + VoteString.GetVoteContent(xs);
            string compY = VoteString.GetVoteTask(ys) + " " + VoteString.GetVoteContent(ys);

            int result = string.Compare(compX, compY, CultureInfo.CurrentUICulture, CompareOptions.IgnoreCase);

            return result;
        }
    }

    public static class MathUtil
    {
        public static T Clamp<T>(T? min, T value, T? max) where T: struct, IComparable<T>
        {
            if (min.HasValue && value.CompareTo(min.Value) < 0)
                return min.Value;

            if (max.HasValue && value.CompareTo(max.Value) > 0)
                return max.Value;

            return value;
        }
    }
}
