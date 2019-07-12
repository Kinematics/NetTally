using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Extensions;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public static class VoteBlocks
    {
        public static IEnumerable<VoteLineBlock> GetBlocks(IEnumerable<VoteLine> lines)
        {
            var blocks = lines.GroupAdjacentToPreviousKey(a => a.Depth == 0, a => a.Content, a => a.Content);

            var blocksOfLines = blocks.Select(b => new VoteLineBlock(b));

            return blocksOfLines;
        }

        public static bool IsThisAContentBlock(IEnumerable<VoteLine> block)
        {
            if (!block.Any())
                return false;

            if (block.First().Depth != 0)
                return false;

            var remainder = block.Skip(1);
            if (!remainder.Any())
                return false;

            if (remainder.All(a => a.Depth > 0))
                return true;

            return false;
        }


        public static (bool isPlan, bool isImplicit, string planName) IsBlockAnExplicitPlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Plan || lineStatus == LineStatus.Proposed)
            {
                var remainder = block.Skip(1);
                isPlan = (firstLine.Depth == 0 && remainder.Any() && remainder.All(a => a.Depth > 0));
            }

            return (isPlan, false, planName);
        }

        public static (bool isPlan, bool isImplicit, string planName) IsBlockAnImplicitPlan(IEnumerable<VoteLine> block)
        {
            if (block.Count() > 1)
            {
                var firstLine = block.First();
                var secondLine = block.Skip(1).First();
                var (lineStatus, planName) = CheckIfPlan(firstLine);

                if (lineStatus == LineStatus.Plan && secondLine.Depth == 0)
                {
                    return (true, true, planName);
                }
            }

            return (false, false, "");
        }

        public static (bool isPlan, bool isImplicit, string planName) IsBlockAnImplicitPlan(IEnumerable<VoteLineBlock> blocks)
        {
            var firstBlock = blocks.First();

            if (firstBlock.Count() == 1 && blocks.Count() > 1)
            {
                var (lineStatus, planName) = CheckIfPlan(firstBlock.First());

                if (lineStatus == LineStatus.Plan)
                {
                    return (true, true, planName);
                }
            }

            return (false, false, "");
        }

        public static (bool isPlan, bool isImplicit, string planName) IsBlockAProposedPlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Proposed)
            {
                var remainder = block.Skip(1);
                isPlan = (firstLine.Depth == 0 && remainder.Any() && remainder.All(a => a.Depth > 0));
            }

            return (isPlan, false, planName);
        }

        public static (bool isPlan, bool isImplicit, string planName) IsBlockASingleLinePlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Plan && block.Count() == 1)
            {
                isPlan = true;
            }

            return (isPlan, false, planName);
        }


        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        static readonly Regex basePlanRegex = new Regex(@"(base|proposed)\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        // Check for a plan reference, alternate format. "Arkatekt's Plan"
        static readonly Regex altPlanRegex = new Regex(@"^(?<planname>.+?)'s\s+plan$", RegexOptions.IgnoreCase);

        public enum LineStatus
        {
            None,
            Plan,
            Proposed
        }

        public static (LineStatus status, string name) CheckIfPlan(VoteLine line)
        {
            Match m;

            m = basePlanRegex.Match(line.CleanContent);
            if (m.Success)
                return (LineStatus.Proposed, m.Groups["planname"].Value.Trim());

            m = anyPlanRegex.Match(line.CleanContent);
            if (m.Success)
                return (LineStatus.Plan, m.Groups["planname"].Value.Trim());

            m = altPlanRegex.Match(line.CleanContent);
            if (m.Success)
                return (LineStatus.Plan, m.Groups["planname"].Value.Trim());

            return (LineStatus.None, string.Empty);
        }

        public static (bool content, bool task) AreEquivalent(List<VoteLine> a, List<VoteLine> b)
        {
            if (a.Count == 0 && b.Count == 0)
                return (true, false);

            if (a.Count == 0 || b.Count == 0)
                return (false, false);

            bool task = Agnostic.StringComparer.Equals(a.First().Task, b.First().Task);

            if (a.Count != b.Count)
                return (false, task);

            return (a.SequenceEquals(b, item => item.CleanContent, Agnostic.StringComparer), task);
        }
    }
}
