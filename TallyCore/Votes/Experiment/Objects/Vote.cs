using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Extensions;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate any vote that can be extracted from a post.
    /// </summary>
    public class Vote
    {
        public Post Post { get; }
        public bool IsValid { get; private set; }
        public string FullText { get; }
        private readonly List<VoteLine> voteLines = new List<VoteLine>();
        public IReadOnlyList<VoteLine> VoteLines { get { return voteLines; } }

        #region Regexes
        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        static readonly Regex tallyRegex = new Regex(@"^#####");
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        // Also allow checkmarks (✓✔), rankings (#1 to #9), scoring (+1 to +9), raw values (1 to 9), and approval (+ or -).
        static readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*(?<marker>[xX✓✔]|[#+]?[1-9]|[-+*])\s*\]");
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^(?<base>base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        #endregion

        #region Constructor
        public Vote(Post post, string message)
        {
            Post = post ?? throw new ArgumentNullException(nameof(post));
            FullText = message ?? throw new ArgumentNullException(nameof(message));
            ProcessMessageLines(message);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get all plan-type components out of the current vote.
        /// </summary>
        /// <returns>Returns a list of the plans contained in the vote.</returns>
        public List<PlanDescriptor> GetPlans()
        {
            if (!IsValid)
                throw new InvalidOperationException("This is not a valid vote.");

            List<PlanDescriptor> planDescriptors = new List<PlanDescriptor>();

            var voteGrouping = voteLines.GroupAdjacentByContinuation(
                source => source.CleanContent,
                GroupContinuationCheck);

            bool checkForBasePlans = true;

            // Base plans
            while (checkForBasePlans && voteGrouping.Any())
            {
                var voteGroup = voteGrouping.First();

                Match m = anyPlanRegex.Match(voteGroup.Key);

                if (m.Groups["base"].Success && voteGroup.First().MarkerType == MarkerType.Vote && voteGroup.Count() > 1)
                {
                    planDescriptors.Add(new PlanDescriptor(PlanType.Base, m.Groups["planname"].Value, voteGroup.ToList()));
                    voteGrouping = voteGrouping.Skip(1);
                }
                else
                {
                    checkForBasePlans = false;
                }
            }

            // Vote labels
            if (voteGrouping.Any())
            {
                var voteGroup = voteGrouping.First();

                Match m = anyPlanRegex.Match(voteGroup.Key);

                if (m.Success && m.Groups["base"].Success == false && voteGroup.Count() == 1 &&
                    voteGroup.First().MarkerType == MarkerType.Vote)
                {
                    var labeledGroups = voteGrouping.TakeWhile(g => g.First().MarkerType == MarkerType.Vote ||
                        g.First().MarkerType == MarkerType.Approval);

                    PlanType labelType = labeledGroups.Skip(1).Any() ? PlanType.Label : PlanType.SingleLine;

                    voteGrouping = voteGrouping.Skip(labeledGroups.Count());

                    planDescriptors.Add(new PlanDescriptor(labelType, m.Groups["planname"].Value,
                        labeledGroups.SelectMany(a => a).ToList()));
                }
            }

            // Any other defined plans with content
            foreach (var voteGroup in voteGrouping)
            {
                Match m = anyPlanRegex.Match(voteGroup.Key);
                if (m.Success && voteGroup.Skip(1).Any())
                {
                    planDescriptors.Add(new PlanDescriptor(PlanType.Content, m.Groups["planname"].Value, voteGroup.ToList()));
                }
            }
            
            // Return all collected plans
            return planDescriptors;
        }

        /// <summary>
        /// Vote lines grouped into blocks.
        /// </summary>
        public List<VoteLineSequence> GetComponents(PartitionMode partitionMode)
        {
            List<VoteLineSequence> bigList = new List<VoteLineSequence>();

            var voteGrouping = voteLines.GroupAdjacentByContinuation(
                source => source.CleanContent,
                GroupContinuationCheck);

            if (partitionMode == PartitionMode.ByLine)
            {
                var transfer = voteGrouping.Select(a => CommuteLines(a));

                bigList.AddRange(transfer.SelectMany(a => a));
            }
            else if (partitionMode == PartitionMode.ByBlock)
            {
                bigList.AddRange(voteGrouping.Select(g => new VoteLineSequence(g)));
            }


            return bigList;
        }

        private List<VoteLineSequence> CommuteLines(IGrouping<string, VoteLine> group)
        {
            var first = group.First();
            string firstTask = first.Task;

            List<VoteLineSequence> results = new List<VoteLineSequence>();

            // Votes and Approvals can be split, and carry their task with them.
            if (first.MarkerType == MarkerType.Vote || first.MarkerType == MarkerType.Approval)
            {
                results.Add(new VoteLineSequence(first));

                foreach (var line in group.Skip(1))
                {
                    if (string.IsNullOrEmpty(firstTask) || !string.IsNullOrEmpty(line.Task))
                        results.Add(new VoteLineSequence(line));
                    else
                        results.Add(new VoteLineSequence(line.Modify(task: firstTask)));
                }
            }
            // Rank doesn't modify any contents, and never gets split.
            else if (first.MarkerType == MarkerType.Rank)
            {
                results.Add(new VoteLineSequence(group.ToList()));
            }
            // Score carries both score and task to child elements.
            else if (first.MarkerType == MarkerType.Score)
            {
                results.Add(new VoteLineSequence(first));

                foreach (var line in group.Skip(1))
                {
                    string newMarker = null;

                    if (line.MarkerType == MarkerType.Vote || line.MarkerType == MarkerType.Continuation)
                    {
                        newMarker = first.Marker;
                    }

                    if (string.IsNullOrEmpty(firstTask) || !string.IsNullOrEmpty(line.Task))
                        results.Add(new VoteLineSequence(line.Modify(marker: newMarker)));
                    else
                        results.Add(new VoteLineSequence(line.Modify(marker: newMarker, task: firstTask)));
                }
            }
            // Anything else just carries the individual lines
            else
            {
                foreach (var line in group)
                {
                    results.Add(new VoteLineSequence(line));
                }
            }
            
            return results;
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
                    voteLines.Clear();
                    break;
                }

                Match m = voteLineRegex.Match(cleanLine);
                if (m.Success)
                {
                    voteLines.Add(new VoteLine(line, cleanLine));
                }
            }

            IsValid = VoteLines.Any();
        }

        /// <summary>
        /// Function to use to determine whether a vote line can be grouped
        /// with an initial vote line.
        /// </summary>
        /// <param name="current">The vote line being checked.</param>
        /// <param name="currentKey">The vote key for the current group.</param>
        /// <param name="initial">The vote line that marks the start of the group.</param>
        /// <returns>Returns true if the current vote line can be added to the group.</returns>
        private bool GroupContinuationCheck(VoteLine current, string currentKey, VoteLine initial)
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
