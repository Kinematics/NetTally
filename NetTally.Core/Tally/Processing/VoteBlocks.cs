using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Models.Comparers;
using NetTally.Models.Creation;
using NetTally.Models.Behavior;
using NetTally.Models.Votes;
using NetTally.Utility.Collections;
using NetTally.Utility.Linq;

namespace NetTally.Tally.Processing;

/// <summary>
/// Static class for functions to analyze blocks of vote lines.
/// </summary>
public static partial class VoteBlocks
{
    #region Plan Name Regexes
    // Check for a vote line that marks a portion of the user's post as a proposed/base plan.
    [GeneratedRegex(@"(base|proposed)\s*plan((:|\s)+)(?<planname>.+)",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    private static partial Regex ProposedPlanRegex();

    // Check for a plan reference. "Plan: Dwarf Raid"
    [GeneratedRegex(@"^plan(:|\s)+◈?@?(?<planname>.+)\.?$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    private static partial Regex AnyPlanRegex();

    // Check for a plan reference, alternate format. "Arkatekt's Plan"
    [GeneratedRegex(@"^(?<planname>.+?)'s\s+plan$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, "en-US")]
    private static partial Regex AltPlanRegex();
    #endregion Plan Name Regexes

    /// <summary>
    /// Convert a list of vote lines into a list of vote blocks.
    /// </summary>
    /// <param name="lines">An enumeration of vote lines.</param>
    /// <returns>The lines are grouped together and turned into blocks.</returns>
    public static IEnumerable<VoteBlock> GetBlocks(IEnumerable<VoteLine> lines)
    {
        var blocks = lines.GroupAdjacentToPreviousKey(
            a => a.Prefix.Depth == 0,
            a => a.Content,
            a => a.Content);

        var blocksOfLines = blocks.Select(VoteBlock.Create)
            .Where(v => v != null)
            .Select(v => v!);

        return blocksOfLines;
    }

    /// <summary>
    /// Determines whether a list of vote lines is structured as a content block.
    /// A content block has a 0 Depth first line, and 1+ Depth on all remaining lines.
    /// </summary>
    /// <param name="block">The lines to examine.</param>
    /// <returns><c>True</c> if the lines represent a content block. Otherwise <c>false</c>.</returns>
    public static bool IsThisAContentBlock(VoteBlock block)
    {
        if (block.LineCount < 2)
            return false;

        if (block.Lines[0].Prefix.Depth != 0)
            return false;

        if (block.Lines.Skip(1).Any(a => a.Prefix.Depth == 0))
            return false;

        return true;
    }

    /// <summary>
    /// Determines whether the provided vote block represents a proposed plan.
    /// A proposed plan specifies "Proposed" in front of the plan name, and
    /// has a content block.
    /// </summary>
    /// <param name="block">The vote block to examine.</param>
    /// <returns>A descriptor indicating whether the block is a plan, whether
    /// it's implicit, and what its name is.</returns>
    public static PlanDescriptor IsBlockAProposedPlan(VoteBlock block)
    {
        if (block.LineCount == 0)
            return PlanDescriptor.None;

        bool isPlan = false;
        var (lineStatus, planName) = CheckIfPlan(block.Lines[0]);

        if (lineStatus == PlanStatus.Proposed)
        {
            isPlan = IsThisAContentBlock(block);
        }

        return new(isPlan, false, planName);
    }

    /// <summary>
    /// An explicit plan has a content block nested beneath the plan name line.
    /// </summary>
    /// <param name="block">The vote block to examine.</param>
    /// <returns>A descriptor indicating whether the block is a plan, whether
    /// it's implicit, and what its name is.</returns>
    public static PlanDescriptor IsBlockAnExplicitPlan(VoteBlock block)
    {
        if (block.LineCount == 0)
            return PlanDescriptor.None;

        bool isPlan = false;
        (PlanStatus PlanStatus, string PlanName) = CheckIfPlan(block.Lines[0]);

        if (PlanStatus == PlanStatus.Plan || PlanStatus == PlanStatus.Proposed)
        {
            isPlan = IsThisAContentBlock(block);
        }

        return new(isPlan, false, PlanName);
    }

    /// <summary>
    /// An implicit plan has the plan name on the first line, and subsequent lines
    /// are considered part of the plan, even without being nested.
    /// </summary>
    /// <param name="block">The vote block to examine.</param>
    /// <returns>A descriptor indicating whether the block is a plan, whether
    /// it's implicit, and what its name is.</returns>
    public static PlanDescriptor IsBlockAnImplicitPlan(VoteBlock block)
    {
        if (block.LineCount > 1)
        {
            var firstLine = block.Lines[0];
            var secondLine = block.Lines[1];
            var (lineStatus, planName) = CheckIfPlan(firstLine);
            var (lineStatus2, _) = CheckIfPlan(secondLine);

            if (lineStatus == PlanStatus.Plan &&
                lineStatus2 != PlanStatus.Plan &&
                secondLine.Prefix.Depth == 0)
            {
                return new(true, true, planName);
            }
        }

        return PlanDescriptor.None;
    }

    /// <summary>
    /// Determines whether the provided vote block represents a single-line plan.
    /// </summary>
    /// <param name="block">The vote block to examine.</param>
    /// <returns>A descriptor indicating whether the block is a plan, whether
    /// it's implicit, and what its name is.</returns>
    public static PlanDescriptor IsBlockASingleLinePlan(VoteBlock block)
    {
        if (block.LineCount == 0)
            return PlanDescriptor.None;

        bool isPlan = false;
        var (lineStatus, planName) = CheckIfPlan(block.Lines[0]);

        if (lineStatus == PlanStatus.Plan && block.LineCount == 1)
        {
            isPlan = true;
        }

        return new(isPlan, false, planName);
    }

    /// <summary>
    /// Determines whether the provided vote line contains elements that are used
    /// to define a plan.
    /// </summary>
    /// <param name="line">The vote line to examine.</param>
    /// <returns>A tuple of whether the line represents a plan, and the plan's name,
    /// if any.</returns>
    public static (PlanStatus PlanStatus, string PlanName) CheckIfPlan(VoteLine line)
    {
        Match m;

        m = ProposedPlanRegex().Match(line.Content.CleanContent);
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

    /// <summary>
    /// Determines whether two provided lists of vote lines are equivalent in terms
    /// of task and content.
    /// </summary>
    /// <param name="x">The first list</param>
    /// <param name="y">The second list</param>
    /// <returns>A tuple describing whether the content and the tasks are equal
    /// between the two lists.</returns>
    public static (bool IsContentEqual, bool IsTaskEqual) AreEquivalent(List<VoteLine> x, List<VoteLine> y)
    {
        if (x.Count == 0 && y.Count == 0)
            return (IsContentEqual: true, IsTaskEqual: false);

        if (x.Count == 0 || y.Count == 0)
            return (IsContentEqual: false, IsTaskEqual: false);

        bool taskIsTheSame = VoteTaskComparer.Instance.Equals(x[0].Task, y[0].Task);

        if (x.Count != y.Count)
            return (IsContentEqual: false, IsTaskEqual: taskIsTheSame);

        bool voteLinesAreTheSame = x.SequenceEquals(y, item => item.Content, VoteContentComparer.Instance);

        return (IsContentEqual: voteLinesAreTheSame, IsTaskEqual: taskIsTheSame);
    }
}
