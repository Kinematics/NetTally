using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Extensions;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tally.ComponentsF.Counting;

/// <summary>
/// Static class for functions to analyze blocks of vote lines.
/// </summary>
public static partial class VoteBlocks
{
    #region Plan Name Regexes
    // Check for a vote line that marks a portion of the user's post as an abstract base plan.
    [GeneratedRegex(@"(base|proposed)\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex BasePlanRegex();

    // Check for a plan reference. "Plan: Dwarf Raid"
    [GeneratedRegex(@"^plan(:|\s)+◈?@?(?<planname>.+)\.?$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AnyPlanRegex();

    // Check for a plan reference, alternate format. "Arkatekt's Plan"
    [GeneratedRegex(@"^(?<planname>.+?)'s\s+plan$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AltPlanRegex();
    #endregion Plan Name Regexes

    /// <summary>
    /// Convert a list of vote lines into a list of vote blocks.
    /// </summary>
    /// <param name="lines">An enumeration of vote lines.</param>
    /// <returns>The lines are grouped together and turned into blocks.</returns>
    public static IEnumerable<VoteBlockType> GetBlocks(IEnumerable<VoteLineType> lines)
    {
        var blocks = lines.GroupAdjacentToPreviousKey(a => a.Prefix.Depth == 0, a => a.Content, a => a.Content);

        var blocksOfLines = blocks.Select(VoteBlock.Create)
            .Where(v => v != null)
            .Select(v => v!);

        return blocksOfLines;
    }

    public static bool IsThisAContentBlock(IEnumerable<VoteLineType> lines)
    {
        if (!lines.Any())
            return false;

        if (lines.First().Prefix.Depth != 0)
            return false;

        var remainder = lines.Skip(1);
        if (!remainder.Any())
            return false;

        if (remainder.All(a => a.Prefix.Depth > 0))
            return true;

        return false;
    }

    /// <summary>
    /// An explicit plan has subsequent lines nested beneath the plan name line.
    /// </summary>
    /// <param name="lines">The block of vote lines</param>
    /// <returns></returns>
    public static (bool isPlan, bool isImplicit, string planName)
        IsBlockAnExplicitPlan(IEnumerable<VoteLineType> lines)
    {
        bool isPlan = false;
        var firstLine = lines.First();
        var (lineStatus, planName) = CheckIfPlan(firstLine);

        if (lineStatus == PlanStatus.Plan || lineStatus == PlanStatus.Proposed)
        {
            var remainder = lines.Skip(1);
            isPlan = firstLine.Prefix.Depth == 0 && remainder.Any() &&
                     remainder.All(a => a.Prefix.Depth > 0);
        }

        return (isPlan, false, planName);
    }

    /// <summary>
    /// An implicit plan has the plan name on the first line, and subsequent lines
    /// are considered part of the plan, even without being nested.
    /// </summary>
    /// <param name="lines">The block of vote lines</param>
    /// <returns></returns>
    public static (bool isPlan, bool isImplicit, string planName)
        IsBlockAnImplicitPlan(IEnumerable<VoteLineType> lines)
    {
        if (lines.Count() > 1)
        {
            var firstLine = lines.First();
            var secondLine = lines.Skip(1).First();
            var (lineStatus, planName) = CheckIfPlan(firstLine);
            var (lineStatus2, _) = CheckIfPlan(secondLine);

            if (lineStatus == PlanStatus.Plan &&
                lineStatus2 != PlanStatus.Plan &&
                secondLine.Prefix.Depth == 0)
            {
                return (true, true, planName);
            }
        }

        return (false, false, "");
    }

    public static (bool isPlan, bool isImplicit, string planName)
        IsBlockAnImplicitPlan(IEnumerable<VoteBlockType> blocks)
    {
        var firstBlock = blocks.First();

        if (firstBlock.Count() == 1 && blocks.Count() > 1)
        {
            var (lineStatus, planName) = CheckIfPlan(firstBlock.First());

            if (lineStatus == PlanStatus.Plan)
            {
                return (true, true, planName);
            }
        }

        return (false, false, "");
    }

    public static (bool isPlan, bool isImplicit, string planName)
        IsBlockAProposedPlan(IEnumerable<VoteLineType> lines)
    {
        bool isPlan = false;
        var firstLine = lines.First();
        var (lineStatus, planName) = CheckIfPlan(firstLine);

        if (lineStatus == PlanStatus.Proposed)
        {
            var remainder = lines.Skip(1);
            isPlan = firstLine.Prefix.Depth == 0 && remainder.Any() &&
                     remainder.All(a => a.Prefix.Depth > 0);
        }

        return (isPlan, false, planName);
    }

    public static (bool isPlan, bool isImplicit, string planName)
        IsBlockASingleLinePlan(IEnumerable<VoteLineType> lines)
    {
        bool isPlan = false;
        var firstLine = lines.First();
        var (lineStatus, planName) = CheckIfPlan(firstLine);

        if (lineStatus == PlanStatus.Plan && lines.Count() == 1)
        {
            // TODO: Make sure a fully realized version of this plan name doesn't already exist.

            isPlan = true;
        }

        return (isPlan, false, planName);
    }

    public static (PlanStatus status, string name) CheckIfPlan(VoteLineType line)
    {
        Match m;

        m = BasePlanRegex().Match(line.Content.CleanContent);
        if (m.Success)
            return (PlanStatus.Proposed, m.Groups["planname"].Value.Trim());

        m = AnyPlanRegex().Match(line.Content.CleanContent);
        if (m.Success)
            return (PlanStatus.Plan, m.Groups["planname"].Value.Trim());

        m = AltPlanRegex().Match(line.Content.CleanContent);
        if (m.Success)
            return (PlanStatus.Plan, m.Groups["planname"].Value.Trim());

        return (PlanStatus.None, string.Empty);
    }

    public static (bool content, bool task) AreEquivalent(List<VoteLineType> a, List<VoteLineType> b)
    {
        if (a.Count == 0 && b.Count == 0)
            return (true, false);

        if (a.Count == 0 || b.Count == 0)
            return (false, false);

        bool taskIsTheSame = VoteTaskComparer.Instance.Equals(a[0].Task, b[0].Task);

        if (a.Count != b.Count)
            return (false, taskIsTheSame);

        bool voteLinesAreTheSame = a.SequenceEquals(b, item => item.Content, VoteContentComparer.Instance);

        return (voteLinesAreTheSame, taskIsTheSame);
    }
}
