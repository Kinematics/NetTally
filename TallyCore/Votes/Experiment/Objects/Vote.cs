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
        public List<VoteLine> VoteLines { get; } = new List<VoteLine>();

        #region Regexes
        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        static readonly Regex tallyRegex = new Regex(@"^#####");
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        // Also allow checkmarks (✓✔), rankings (#1 to #9), scoring (+1 to +9), raw values (1 to 9), and approval (+ or -).
        static readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*(?<marker>[xX✓✔]|[#+]?[1-9]|[-+*])\s*\]");
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^(?<base>base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        #endregion

        public Vote(Post post, string message)
        {
            Post = post ?? throw new ArgumentNullException(nameof(post));
            FullText = message ?? throw new ArgumentNullException(nameof(message));
            ProcessMessage(message);
        }

        /// <summary>
        /// Extract any vote lines from the message text, and save both the original and
        /// the cleaned (no BBCode) in a list.
        /// Do not record any vote lines if there's a tally marker (#####).
        /// Mark the vote as valid if it has any vote lines.
        /// </summary>
        /// <param name="message">The original, full message text.</param>
        private void ProcessMessage(string message)
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
                    VoteLines.Add(new VoteLine(line, cleanLine));
                }
            }

            IsValid = VoteLines.Any();
        }

        /// <summary>
        /// Get all plans out of the current vote.
        /// </summary>
        /// <returns>Returns a list of the plans contained in the vote.</returns>
        public List<PlanDescriptor> GetPlans()
        {
            if (!IsValid)
                throw new InvalidOperationException("This is not a valid vote.  Cannot get plans.");

            List<PlanDescriptor> planDescriptors = new List<PlanDescriptor>();

            var voteGrouping = VoteLines.GroupAdjacentByContinuation(
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
                return (current.Prefix.Length > 0 && current.MarkerType == MarkerType.Continuation);
            }
            else if (initial.MarkerType == MarkerType.Score)
            {
                return (current.Prefix.Length > 0 &&
                    (current.MarkerType == MarkerType.Continuation || current.MarkerType == MarkerType.Score));
            }

            return false;
        }


        /// <summary>
        /// Vote lines grouped into blocks.
        /// </summary>
        public IEnumerable<VoteLine> VoteBlocks
        {
            get
            {
                var voteBlocks = VoteLines.GroupAdjacentByComparison(anchor => anchor.CleanContent, (next, currentKey) => string.IsNullOrEmpty(next.Prefix));

                foreach (var block in voteBlocks)
                    yield return block as VoteLine;
            }
        }
    }
}
