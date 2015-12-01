using System;
using System.Collections;
using System.Collections.Generic;
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

            string compX = VoteString.GetVoteTask(xs) + " " + VoteString.GetVoteContent(xs);
            string compY = VoteString.GetVoteTask(ys) + " " + VoteString.GetVoteContent(ys);

            int result = string.Compare(compX, compY, CultureInfo.CurrentUICulture, CompareOptions.IgnoreCase);

            return result;
        }
    }

    public class CustomStringComparer : IComparer, IEqualityComparer, IComparer<string>, IEqualityComparer<string>
    {
        private CompareInfo myComp;
        private CompareOptions myOptions = CompareOptions.None;

        // Constructs a comparer using the specified CompareOptions.
        public CustomStringComparer(CompareInfo cmpi, CompareOptions options)
        {
            myComp = cmpi;
            this.myOptions = options;
        }

        // Compares strings with the CompareOptions specified in the constructor.
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            return myComp.Compare(x, y, myOptions);
        }

        public bool Equals(string x, string y) => this.Compare(x, y) == 0;

        public int GetHashCode(string obj) => obj.GetHashCode();


        // Compares strings with the CompareOptions specified in the constructor.
        int IComparer.Compare(object x, object y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            String sx = x as String;
            String sy = y as String;
            if (sx != null && sy != null)
                return myComp.Compare(sx, sy, myOptions);
            throw new ArgumentException("x and y should be strings.");
        }

        bool IEqualityComparer.Equals(object x, object y) => ((IComparer)this).Compare(x, y) == 0;

        int IEqualityComparer.GetHashCode(object obj) => (obj as string)?.GetHashCode() ?? 0;

    }
    
}
