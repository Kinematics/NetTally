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

            // If there are any base plan lines, collect those.
            if (BasePlansLines.Any())
            {
                // Group lines where the group starts each time there's no prefix value.
                var basePlanGrouping = BasePlansLines.GroupAdjacentByComparison(anchor => anchor.CleanContent, (next, currentKey) => string.IsNullOrEmpty(next.Prefix)).ToList();

                foreach (var plan in basePlanGrouping)
                {
                    Match m = anyPlanRegex.Match(plan.Key);
                    if (m.Success)
                        planDescriptors.Add(new PlanDescriptor(PlanType.Base, m.Groups["planname"].Value, plan.ToList()));
                }
            }

            // Group lines where the group starts each time there's no prefix value.
            var voteGrouping = VoteLines.GroupAdjacentByComparison(anchor => anchor.CleanContent, (next, currentKey) => string.IsNullOrEmpty(next.Prefix));

            // Check for any plan labels (where the first line is a plan name, but group has no content).
            if (voteGrouping.Any())
            {
                var firstGroup = voteGrouping.First();

                if (firstGroup.Count() == 1)
                {
                    Match m = anyPlanRegex.Match(firstGroup.Key);
                    if (m.Success)
                    {
                        var labelPlan = voteGrouping.First();
                        voteGrouping = voteGrouping.Skip(1);

                        // Check for full plan label vs just a single line vote.
                        PlanType labelType = voteGrouping.Any() ? PlanType.Label : PlanType.SingleLine;

                        planDescriptors.Add(new PlanDescriptor(labelType, m.Groups["planname"].Value, labelPlan.ToList()));
                    }
                }
            }

            // Next, check for any embedded plans with content.
            foreach (var group in voteGrouping)
            {
                if (group.Count() > 1)
                {
                    Match m = anyPlanRegex.Match(group.Key);
                    if (m.Success)
                    {
                        planDescriptors.Add(new PlanDescriptor(PlanType.Content, m.Groups["planname"].Value, group.ToList()));
                    }
                }
            }

            return planDescriptors;
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
