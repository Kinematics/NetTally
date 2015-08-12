using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public class Vote : IComparable, IComparer<Vote>
    {
        public VoteType VoteType { get; }
        public List<PostLine> VoteLines { get; }

        public Vote(IEnumerable<PostLine> voteLines, VoteType voteType)
        {
            VoteLines = voteLines.ToList();
            VoteType = voteType;
        }

        public Vote(PostLine voteLine, VoteType voteType)
        {
            VoteLines = new List<PostLine>() { voteLine };
            VoteType = voteType;
        }

        /// <summary>
        /// Return the minimized string of all the vote lines in the vote.
        /// </summary>
        /// <returns></returns>
        public string Minimized()
        {
            if (VoteLines.Count == 1)
            {
                return VoteString.MinimizeVote(VoteLines.First().Clean, PartitionMode.ByLine);
            }

            StringBuilder sb = new StringBuilder();

            foreach (var line in VoteLines)
            {
                sb.Append(VoteString.MinimizeVote(line.Clean, PartitionMode.None));
            }

            return sb.ToString();
        }

        /// <summary>
        /// IComparer function.
        /// </summary>
        /// <param name="x">The first object being compared.</param>
        /// <param name="y">The second object being compared.</param>
        /// <returns>Returns a negative value if x is 'before' y, 0 if they're equal, and
        /// a positive value if x is 'after' y.</returns>
        public int Compare(Vote x, Vote y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            return string.Compare(x.Minimized(), y.Minimized());
        }

        /// <summary>
        /// IComparable function.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Returns a negative value if this is 'before' y, 0 if they're equal, and
        /// a positive value if this is 'after' y.</returns>
        public int CompareTo(object obj)
        {
            return Compare(this, obj as Vote);
        }
    }
}
