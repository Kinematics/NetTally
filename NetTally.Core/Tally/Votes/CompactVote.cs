using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Utility;

namespace NetTally.Votes
{
    // Individual dictionary element from VoteStorage:
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;
    using VoterStorageEntry = KeyValuePair<Origin, VoteLineBlock>;

    /// <summary>
    /// A compact vote allows multiple votes to be displayed as a collective when
    /// the lines of each vote match up with each other.
    /// </summary>
    public class CompactVote : IComparable, IComparable<CompactVote>, IEquatable<CompactVote>
    {
        #region Construction
        /// <summary>
        /// Get a list of compact voter information from the provided votes.
        /// </summary>
        /// <param name="votes">The votes to get compact voters from.</param>
        /// <returns>Returns the series of compact votes.</returns>
        public static IEnumerable<CompactVote> GetCompactVotes(IEnumerable<VoteStorageEntry> votes)
        {
            if (votes == null || !votes.Any())
                return Enumerable.Empty<CompactVote>();

            // Group votes by first vote line, as that's the basis for further consolidation.
            var groupedVotes = votes.GroupBy(v => v.Key.Lines.First());

            List<CompactVote> compactVotes = new List<CompactVote>();

            foreach (var group in groupedVotes)
            {
                // Get the depth 0 lines from each vote.
                var childLines = GetChildLinesOfLine(group.Key, group, topLevel: true);

                compactVotes.Add(new CompactVote(group.Key, childLines, group, parent: null));
            }

            return compactVotes;
        }

        /// <summary>
        /// Create a child CompactVote based on the provided vote line and filtered voters.
        /// </summary>
        /// <param name="childLine">The vote line that's the top of the creation tree.</param>
        /// <param name="votes">Votes that were part of the parent CompactVote.</param>
        /// <param name="parent">The parent of the CompactVote being created.</param>
        /// <returns>Returns a compact vote built on the child line provided.</returns>
        private CompactVote RecursiveCreation(VoteLine childLine, IEnumerable<VoteStorageEntry> votes,
            CompactVote parent)
        {
            // Get the children for the next layer of the tree.
            var childLines = GetChildLinesOfLine(childLine, votes);

            // Filter the voters to only those that contain the current child line.
            votes = votes.Where(v => v.Key.Lines.Contains(childLine));

            return new CompactVote(childLine, childLines, votes, parent);
        }

        /// <summary>
        /// Utility function to get all the direct children of the provided vote line.
        /// </summary>
        /// <param name="key">The parent vote line.</param>
        /// <param name="voteGroup">The collection of all votes to be considered.</param>
        /// <param name="topLevel">Whether this is a request from the top level of the vote.</param>
        /// <returns>Returns a list of all direct descendents of the provided vote line.</returns>
        private static IEnumerable<VoteLine> GetChildLinesOfLine(VoteLine key,
            IEnumerable<VoteStorageEntry> voteGroup, bool topLevel = false)
        {
            List<VoteStorageEntry> voteGroupList = new List<VoteStorageEntry>(voteGroup);
            List<VoteLine> holding = new List<VoteLine>();
            List<VoteLine> tempHolding = new List<VoteLine>();

            foreach (var (vote, voteSupport) in voteGroupList)
            {
                tempHolding.Clear();
                int index = vote.Lines.IndexOf(key);

                if (index >= 0)
                {
                    for (int i = index + 1; i < vote.Lines.Count; i++)
                    {
                        if (vote.Lines[i].Depth > key.Depth || (topLevel && vote.Lines[i].Depth == 0))
                        {
                            tempHolding.Add(vote.Lines[i]);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                holding.AddRange(tempHolding.WithMin(a => a.Depth));

            }

            return holding.Distinct();
        }

        /// <summary>
        /// Constructor for a CompactVote.
        /// </summary>
        /// <param name="currentLine">The line being stored in this CompactVote.</param>
        /// <param name="childLines">Any child lines of the current vote line.</param>
        /// <param name="voteGroup">The list of all votes and voters that support the current vote line.</param>
        /// <param name="parent">The parent CompactVote in the vote tree.</param>
        private CompactVote(VoteLine currentLine,
            IEnumerable<VoteLine> childLines,
            IEnumerable<VoteStorageEntry> voteGroup,
            CompactVote parent)
        {
            Parent = parent;
            CurrentLine = currentLine;

            var voters = voteGroup.SelectMany(v => v.Value);
            Voters.AddRange(voters);
            VoterCount = voters.Distinct().Count();

            Children.AddRange(childLines.Select(child => RecursiveCreation(child, voteGroup, this))
                .OrderByDescending(c => c.VoterCount).ThenBy(c => c.CurrentLine));
        }
        #endregion

        #region Properties
        CompactVote Parent { get; }
        public VoteLine CurrentLine { get; }
        public List<CompactVote> Children { get; } = new List<CompactVote>();
        public List<VoterStorageEntry> Voters { get; } = new List<VoterStorageEntry>();
        public int VoterCount { get; }
        #endregion

        /// <summary>
        /// Get the vote tree of the compact vote flattened out.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CompactVote> GetFlattenedCompactVote()
        {
            yield return this;

            foreach (var childElement in Children.SelectMany(a => a.GetFlattenedCompactVote()))
            {
                yield return childElement;
            }
        }

        /// <summary>
        /// Formats the current object as a string.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public override string ToString()
        {
            string result = CurrentLine.ToString();

            if (Children.Any())
            {
                string aggregate = Children.Select(s => s.ToString()).Aggregate((a, b) => $"{a}\n{b}");

                result = $"{result}\n{aggregate}";
            }

            return result;
        }

        /// <summary>
        /// Creates a string that displays the cleaned content, and without any particular marker.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToComparableString()
        {
            string result = CurrentLine.ToComparableString();

            if (Children.Any())
            {
                string aggregate = Children.Select(s => s.ToComparableString()).Aggregate((a, b) => $"{a}\n{b}");

                result = $"{result}\n{aggregate}";
            }

            return result;
        }

        /// <summary>
        /// Creates a string that displays the full vote line content, using the specified marker
        /// instead of the intrinsic vote line's.
        /// </summary>
        /// <param name="displayMarker">The marker to use in the generated output.</param>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToOverrideString(string displayMarker = null, string displayTask = null)
        {
            string result = CurrentLine.ToOverrideString(displayMarker, displayTask);

            if (Children.Any())
            {
                string aggregate = Children.Select(s => s.ToOverrideString(displayMarker, displayTask)).Aggregate((a, b) => $"{a}\n{b}");

                result = $"{result}\n{aggregate}";
            }

            return result;
        }

        /// <summary>
        /// Formats a vote line for output, with optional override marker and task.
        /// Output string only prints the current compact vote, not the children.
        /// </summary>
        /// <param name="displayMarker">The marker to use when displaying the vote line as a string.</param>
        /// <param name="displayTask">The task to use when displaying the vote line as a string.
        /// Will use the default task if left null.</param>
        /// <returns>Returns a string representing the current vote line.</returns>
        public string ToOutputString(string displayMarker = "X", string displayTask = null)
        {
            string result = CurrentLine.ToOutputString(displayMarker, displayTask);

            //if (Children.Any())
            //{
            //    string aggregate = Children.Select(s => s.ToOutputString(displayMarker, displayTask)).Aggregate((a, b) => $"{a}\n{b}");

            //    result = $"{result}\n{aggregate}";
            //}

            return result;
        }



        #region Equality and Comparison
        public static int Compare(CompactVote left, CompactVote right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            return left.CurrentLine.CompareTo(right.CurrentLine);
        }

        public int CompareTo(CompactVote other) => Compare(this, other);
        public int CompareTo(object obj) => Compare(this, obj as CompactVote);
        public bool Equals(CompactVote other) => Compare(this, other) == 0;
        public override bool Equals(object obj) => Compare(this, obj as CompactVote) == 0;

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator >(CompactVote first, CompactVote second) => Compare(first, second) == 1;
        public static bool operator <(CompactVote first, CompactVote second) => Compare(first, second) == -1;
        public static bool operator >=(CompactVote first, CompactVote second) => Compare(first, second) >= 0;
        public static bool operator <=(CompactVote first, CompactVote second) => Compare(first, second) <= 0;
        public static bool operator ==(CompactVote first, CompactVote second) => Compare(first, second) == 0;
        public static bool operator !=(CompactVote first, CompactVote second) => Compare(first, second) != 0;
        #endregion Equality and Comparison
    }
}
