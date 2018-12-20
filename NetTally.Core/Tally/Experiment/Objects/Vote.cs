using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Extensions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate any vote that can be extracted from a post.
    /// </summary>
    class Vote
    {
        #region Properties
        /// <summary>
        /// Link back to the originating post.
        /// </summary>
        public Post Post { get; }
        /// <summary>
        /// Returns true if the vote is valid.
        /// </summary>
        public bool IsValid { get; private set; }
        /// <summary>
        /// Gets the full original message text.
        /// </summary>
        public string FullText { get; }
        /// <summary>
        /// The vote lines
        /// </summary>
        public VoteLines VoteLines { get; } = new VoteLines();

        public List<VotePartition> WorkingPartitions { get; } = new List<VotePartition>();

        private readonly List<Plan> plans = new List<Plan>();
        #endregion

        #region Regexes
        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        static readonly Regex tallyRegex = new Regex(@"^#####");
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        // Also allow checkmarks (✓✔), rankings (#1 to #9), scoring (+1 to +9), raw values (1 to 9), and approval (+ or -).
        static readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*(?<marker>[xX✓✔]|[#+]?[1-9]|[-+*])\s*\]");
        #endregion

        #region Constructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="Vote"/> class.
        /// </summary>
        /// <param name="post">The originating post.</param>
        /// <param name="message">The message text of the post.</param>
        public Vote(Post post, string message)
        {
            Post = post ?? throw new ArgumentNullException(nameof(post));
            FullText = message ?? throw new ArgumentNullException(nameof(message));
            ProcessMessageLines(message);
        }
        #endregion

        #region Utility methods
        /// <summary>
        /// Extract any vote lines from the message text, and save both the original and
        /// the cleaned (no BBCode) in a list.
        /// Do not record any vote lines if there's a tally marker (#####).
        /// Mark the vote as valid if it has any vote lines.
        /// </summary>
        /// <param name="message">The original, full message text.</param>
        private void ProcessMessageLines(string message)
        {
            var messageLines = message.GetStringLines();

            foreach (var line in messageLines)
            {
                string cleanLine = VoteString.RemoveBBCode(line);

                if (tallyRegex.Match(cleanLine).Success)
                {
                    // If this is a tally post, clear any found vote lines and end processing.
                    VoteLines.Clear();
                    break;
                }

                Match m = voteLineRegex.Match(cleanLine);
                if (m.Success)
                {
                    VoteLines.Add(new VoteLine(line));
                }
            }

            VoteLines.IsReadonly = true;
            IsValid = VoteLines.Any();
        }
        #endregion

        #region Public Methods

        public void SetWorkingVote(List<VotePartition> workingVote)
        {
            WorkingPartitions.Clear();
            WorkingPartitions.AddRange(workingVote);
        }

        /// <summary>
        /// Gets the vote grouped by valid marker type.
        /// </summary>
        /// <returns>Returns the vote broken up into groups based on marker type.</returns>
        public IEnumerable<IGrouping<string, VoteLine>> GetVoteMarkerGroups()
        {
            var voteGrouping = VoteLines.GroupAdjacentToPreviousSource(
                source => source.ComparableContent,
                VoteBlockContinues);

            return voteGrouping;
        }

        public void StorePlans(List<Plan> potentialPlans)
        {
            plans.Clear();

            if (potentialPlans == null)
                return;

            plans.AddRange(potentialPlans);
        }

        /// <summary>
        /// Vote lines grouped into blocks.
        /// </summary>
        public List<VoteLines> GetComponents(PartitionMode partitionMode)
        {
            List<VoteLines> bigList = new List<VoteLines>();

            var voteGrouping = GetVoteMarkerGroups();

            if (partitionMode == PartitionMode.ByLine)
            {
                var transfer = voteGrouping.Select(a => CommuteLines(a));

                bigList.AddRange(transfer.SelectMany(a => a));
            }
            else if (partitionMode == PartitionMode.ByBlock)
            {
                bigList.AddRange(voteGrouping.Select(g => new VoteLines(g)));
            }


            return bigList;
        }

        private List<VoteLines> CommuteLines(IGrouping<string, VoteLine> group)
        {
            var first = group.First();
            string firstTask = first.Task;

            List<VoteLines> results = new List<VoteLines>();

            // Votes and Approvals can be split, and carry their task with them.
            if (first.MarkerType == MarkerType.Vote || first.MarkerType == MarkerType.Approval)
            {
                results.Add(new VoteLines(first));

                foreach (var line in group.Skip(1))
                {
                    if (string.IsNullOrEmpty(firstTask) || !string.IsNullOrEmpty(line.Task))
                        results.Add(new VoteLines(line));
                    else
                        results.Add(new VoteLines(line.Modify(task: firstTask)));
                }
            }
            // Rank doesn't modify any contents, and never gets split.
            else if (first.MarkerType == MarkerType.Rank)
            {
                results.Add(new VoteLines(group.ToList()));
            }
            // Score carries both score and task to child elements.
            else if (first.MarkerType == MarkerType.Score)
            {
                results.Add(new VoteLines(first));

                foreach (var line in group.Skip(1))
                {
                    string? newMarker = null;

                    if (line.MarkerType == MarkerType.Vote || line.MarkerType == MarkerType.Continuation)
                    {
                        newMarker = first.Marker;
                    }

                    if (string.IsNullOrEmpty(firstTask) || !string.IsNullOrEmpty(line.Task))
                        results.Add(new VoteLines(line.Modify(marker: newMarker)));
                    else
                        results.Add(new VoteLines(line.Modify(marker: newMarker, task: firstTask)));
                }
            }
            // Anything else just carries the individual lines
            else
            {
                foreach (var line in group)
                {
                    results.Add(new VoteLines(line));
                }
            }

            return results;
        }

        #endregion

        #region Static utility methods
        /// <summary>
        /// Function to use to determine whether a vote line can be grouped
        /// with an initial vote line.
        /// </summary>
        /// <param name="initial">The vote line that marks the start of the group.</param>
        /// <param name="current">The vote line being checked.</param>
        /// <returns>Returns true if the current vote line can be added to the group.</returns>
        public static bool VoteBlockContinues(VoteLine initial, VoteLine current)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            if (initial == null)
            {
                return false;
            }
            else if (initial.MarkerType == MarkerType.Vote || initial.MarkerType == MarkerType.Approval)
            {
                return (current.Prefix.Length > 0 && current.MarkerType == initial.MarkerType);
            }
            else if (initial.MarkerType == MarkerType.Rank)
            {
                return (current.Prefix.Length > 0 && 
                    (current.MarkerType == MarkerType.Continuation || current.MarkerType == MarkerType.Vote));
            }
            else if (initial.MarkerType == MarkerType.Score)
            {
                return (current.Prefix.Length > 0 &&
                    (current.MarkerType == MarkerType.Continuation || current.MarkerType == MarkerType.Score || current.MarkerType == MarkerType.Vote));
            }

            return false;
        }
        #endregion
    }
}
