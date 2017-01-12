using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Extensions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class Vote
    {
        public bool IsValid { get; private set; }
        public string FullText { get; }
        public List<VoteLine> VoteLines { get; } = new List<VoteLine>();


        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        static readonly Regex tallyRegex = new Regex(@"^#####");
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        static readonly Regex voteLineRegex = new Regex(@"^[-\s]*\[\s*(?<marker>(?<vote>[xX✓✔])|(?<value>[#+]?[1-9])|(?<approval>[+-]))\s*\]");
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^(?<base>base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);


        public Vote(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));

            FullText = message;
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

            bool isTally = false;

            foreach (var line in messageLines)
            {
                string cleanLine = VoteString.RemoveBBCode(line);

                if (tallyRegex.Match(cleanLine).Success)
                {
                    isTally = true;
                    break;
                }

                Match m = voteLineRegex.Match(cleanLine);
                if (m.Success)
                {
                    VoteLines.Add(new VoteLine(line, cleanLine));
                }
            }

            if (isTally)
            {
                VoteLines.Clear();
            }

            IsValid = VoteLines.Any();
        }


        public List<PlanDescriptor> GetPlans()
        {
            List<PlanDescriptor> planDescriptors = new List<PlanDescriptor>();

            if (!VoteLines.Any())
                return planDescriptors;

            // Group lines where the group starts each time there's no prefix value.
            var voteGrouping = VoteLines.GroupAdjacentByComparison(anchor => anchor.CleanContent, (next, currentKey) => string.IsNullOrEmpty(next.Prefix)).ToList();

            var basePlans = voteGrouping.TakeWhile(g => anyPlanRegex.Match(g.Key).Groups["base"].Success).ToList();

            foreach (var plan in basePlans)
            {
                Match m = anyPlanRegex.Match(plan.Key);
                if (m.Success)
                    planDescriptors.Add(new PlanDescriptor(PlanType.Base, m.Groups["planname"].Value, plan.ToList()));
            }

            var remainingGroups = voteGrouping.Skip(basePlans.Count);

            if (remainingGroups.Any())
            {
                var firstGroup = remainingGroups.First();

                if (firstGroup.Count() == 1)
                {
                    Match m = anyPlanRegex.Match(firstGroup.Key);
                    if (m.Success)
                    {
                        var labelPlan = remainingGroups.First();
                        remainingGroups = remainingGroups.Skip(1);

                        PlanType labelType = remainingGroups.Any() ? PlanType.Label : PlanType.SingleLine;

                        planDescriptors.Add(new PlanDescriptor(labelType, m.Groups["planname"].Value, labelPlan.ToList()));
                    }
                }
            }

            foreach (var group in remainingGroups)
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


    public class PlanDescriptor
    {
        public PlanType PlanType { get; }
        public string Name { get; }
        public List<VoteLine> Lines { get; }

        public PlanDescriptor(PlanType planType, string name, List<VoteLine> lines)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            PlanType = planType;
            Name = name;
            Lines = lines ?? throw new ArgumentNullException(nameof(lines));
        }
    }
}
