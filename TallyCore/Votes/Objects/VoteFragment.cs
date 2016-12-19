using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;
using NetTally.Extensions;

namespace NetTally.Votes
{
    public class VoteFragment
    {
        public VoteLine Parent { get; }
        public List<VoteLine> Children { get; } = new List<VoteLine>();

        static readonly Regex hyphens = new Regex("-");

        public VoteFragment(VoteLine line, List<VoteLine> children)
        {
            Parent = line ?? throw new ArgumentNullException(nameof(line));

            if (children != null)
                Children.AddRange(children);
        }

        VoteFragment(IList<string> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (lines.Count == 0)
                throw new ArgumentException("No lines provided");

            Parent = VoteLine.Create(lines.First());

            Children.AddRange(lines.Skip(1).Select(a => VoteLine.Create(a)));
        }

        public static List<VoteFragment> GetFragments(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentNullException(nameof(input));

            var lines = StringUtility.GetStringLines(input);

            return GetFragments(lines);
        }

        public static List<VoteFragment> GetFragments(List<string> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            List<VoteFragment> fragmentList = new List<VoteFragment>();

            var groupedLines = lines.GroupBlocks(s => IndentCount(s));

            foreach (var group in groupedLines)
            {
                fragmentList.Add(new VoteFragment(group));
            }

            return fragmentList;
        }

        private static int IndentCount(string input)
        {
            var prefix = VoteString.GetVotePrefix(input);

            var m = hyphens.Matches(prefix);
            return m.Count;
        }

        #region Class overrides
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Parent.ToString());
            foreach (var child in Children)
            {
                sb.Append("\n");
                sb.Append(child.ToString());
            }

            return sb.ToString();
        }

        public override int GetHashCode()
        {
            int hash = Parent.GetHashCode();
            foreach (var child in Children)
            {
                hash = hash ^ child.GetHashCode();
            }

            return hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is VoteFragment other)
            {
                if (!Parent.Equals(other.Parent))
                    return false;

                if (Children.Count == 0 && other.Children.Count == 0)
                    return true;

                if (Children.Count != other.Children.Count)
                    return false;

                var compareChildren = Children.Zip(other.Children, (F1, F2) => F1.Equals(F2));

                return compareChildren.All(c => c == true);
            }

            return false;
        }
        #endregion
    }

}
