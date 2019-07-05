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
        public static IEnumerable<VoteLineBlock> GetBlocks(List<VoteLine> lines)
        {
            var blocks = lines.GroupAdjacentToPreviousKey(a => a.Depth == 0, a => a.Content, a => a.Content);

            var blocksOfLines = blocks.Select(b => new VoteLineBlock(b));

            return blocksOfLines;
        }

        public static (bool isPlan, string planName) IsBlockAnExplicitPlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Plan || lineStatus == LineStatus.BasePlan)
            {
                var remainder = block.Skip(1);
                isPlan = (firstLine.Depth == 0 && remainder.Any() && remainder.All(a => a.Depth > 0));
            }

            return (isPlan, planName);
        }

        public static (bool isPlan, string planName) IsBlockAnImplicitPlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Plan)
            {
                var secondLine = block.Skip(1).FirstOrDefault();
                isPlan = (secondLine != null && secondLine.Depth == 0);
            }

            return (isPlan, planName);
        }

        public static (bool isPlan, string planName) IsBlockABasePlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.BasePlan)
            {
                var remainder = block.Skip(1);
                isPlan = (firstLine.Depth == 0 && remainder.Any() && remainder.All(a => a.Depth > 0));
            }

            return (isPlan, planName);
        }

        public static (bool isPlan, string planName) IsBlockASingleLinePlan(IEnumerable<VoteLine> block)
        {
            bool isPlan = false;
            var firstLine = block.First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);

            if (lineStatus == LineStatus.Plan && block.Count() == 1)
            {
                isPlan = true;
            }

            return (isPlan, planName);
        }


        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        static readonly Regex basePlanRegex = new Regex(@"(base|proposed)\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference. "Plan: Dwarf Raid"
        static readonly Regex anyPlanRegex = new Regex(@"^plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        // Check for a plan reference, alternate format. "Arkatekt's Plan"
        static readonly Regex altPlanRegex = new Regex(@"^(?<planname>.+?)'s\s+plan$", RegexOptions.IgnoreCase);

        private enum LineStatus
        {
            None,
            Plan,
            BasePlan
        }

        private static (LineStatus status, string name) CheckIfPlan(VoteLine line)
        {
            Match m;
            string content = line.Content.RemoveBBCode().DeUrlContent();

            m = basePlanRegex.Match(content);
            if (m.Success)
                return (LineStatus.BasePlan, m.Groups["planname"].Value.Trim());

            m = anyPlanRegex.Match(content);
            if (m.Success)
                return (LineStatus.Plan, m.Groups["planname"].Value.Trim());

            m = altPlanRegex.Match(content);
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

            var zip = a.Zip(b, (x,y) => new { x, y });

            bool content = true;

            foreach (var z in zip)
            {
                if (!Agnostic.StringComparer.Equals(z.x.Content, z.y.Content))
                {
                    content = false;
                    break;
                }
            }

            return (content, task);
        }
    }
}
