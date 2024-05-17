using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NetTally.Extensions;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Utility;

namespace NetTally.Tally.ComponentsF.Votes;
public record CompactVoteType(
    VoteLineType Line,
    CompactVoteType Parent,
    List<CompactVoteType> Children,
    List<VoterStorageEntryF> Voters)
{
    public int VoterCount { get; set; }
}


public static class CompactVote
{
    public static CompactVoteType None { get; } = new CompactVoteType(VoteLine.Empty, null!, [], []);

    /// <summary>
    /// Get a list of compact voter information from the provided votes.
    /// </summary>
    /// <param name="votes">The votes to get compact voters from.</param>
    /// <returns>Returns the series of compact votes.</returns>
    public static IEnumerable<CompactVoteType> GetCompactVotes(IEnumerable<VoteStorageEntryF> votes)
    {
        // Group votes by first vote line, as that's the basis for further consolidation.
        return votes.GroupBy(v => v.Key.Lines[0])
                    .Select(g => Create(
                                line: g.Key,
                                parent: None,
                                children: GetChildLinesOfLine(g.Key, g, topLevel: true),
                                voteGroups: g));
    }

    public static CompactVoteType Create(VoteLineType line,
        CompactVoteType? parent,
        IEnumerable<VoteLineType> children,
        IEnumerable<VoteStorageEntryF> voteGroups)
    {
        parent ??= None;

        var compactVote = new CompactVoteType(line, parent, [], []);

        var voters = voteGroups.SelectMany(v => v.Value);
        compactVote.Voters.AddRange(voters);

        compactVote.VoterCount = voters
            .DistinctBy(v => v.Key, OriginComparer.Instance)
            .Count();

        var childrenToAdd = children
            .Select(child => RecursiveCreation(child, voteGroups, compactVote))
            .OrderByDescending(c => c.VoterCount)
            .ThenBy(c => c, CompactVoteComparer.Instance);

        compactVote.Children.AddRange(childrenToAdd);

        return compactVote;
    }

    /// <summary>
    /// Create a child CompactVote based on the provided vote line and filtered voters.
    /// </summary>
    /// <param name="childLine">The vote line that's the top of the creation tree.</param>
    /// <param name="votes">Votes that were part of the parent CompactVote.</param>
    /// <param name="parent">The parent of the CompactVote being created.</param>
    /// <returns>Returns a compact vote built on the child line provided.</returns>
    private static CompactVoteType RecursiveCreation(
        VoteLineType childLine,
        IEnumerable<VoteStorageEntryF> votes,
        CompactVoteType parent)
    {
        // Get the children for the next layer of the tree.
        var childLines = GetChildLinesOfLine(childLine, votes);

        // Filter the voters to only those that contain the current child line.
        votes = votes.Where(v => v.Key.Lines.Contains(childLine));

        return Create(childLine, parent, childLines, votes);
    }

    /// <summary>
    /// Utility function to get all the direct children of the provided vote line.
    /// </summary>
    /// <param name="key">The parent vote line.</param>
    /// <param name="voteGroup">The collection of all votes to be considered.</param>
    /// <param name="topLevel">Whether this is a request from the top level of the vote.</param>
    /// <returns>Returns a list of all direct descendents of the provided vote line.</returns>
    private static IEnumerable<VoteLineType> GetChildLinesOfLine(
        VoteLineType key,
        IEnumerable<VoteStorageEntryF> voteGroup,
        bool topLevel = false)
    {
        List<VoteStorageEntryF> voteGroupList = new(voteGroup);
        List<VoteLineType> holding = [];
        List<VoteLineType> tempHolding = [];

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
}

public static class CompactVoteTransform
{
    public static IEnumerable<CompactVoteType> Flatten(CompactVoteType compactVote)
    {
        List<CompactVoteType> list = [compactVote];

        var result = list.Concat(compactVote.Children.SelectMany(Flatten));

        return result;
    }
}

public static class CompactVoteDisplay
{
    public static string ToString(CompactVoteType compactVote)
    {
        return "";
    }

    public static string ToComparableString(CompactVoteType compactVote)
    {
        return "";
    }

    public static string ToOverrideString(
        CompactVoteType compactVote,
        MarkerData? marker = null,
        VoteTaskType? task = null)
    {
        marker ??= Marker.Empty;
        task ??= VoteTask.Empty;

        return "";
    }

    public static string ToOutputString(
        CompactVoteType compactVote,
        string? marker = null,
        string? task = null)
    {
        marker ??= Strings.VoteMarker;
        task ??= string.Empty;

        return "";
    }
}

public class CompactVoteComparer : IEqualityComparer<CompactVoteType>, IComparer<CompactVoteType>
{
    public static CompactVoteComparer Instance { get; } = new CompactVoteComparer();

    public int Compare(CompactVoteType? x, CompactVoteType? y)
    {
        if (ReferenceEquals(x, y))
            return 0;
        if (x is null)
            return -1;
        if (y is null)
            return 1;

        return VoteLineComparer.Instance.Compare(x.Line, y.Line);
    }

    public bool Equals(CompactVoteType? x, CompactVoteType? y)
    {
        if (x == null || y == null)
        {
            return false;
        }

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] CompactVoteType obj)
    {
        return VoteLineComparer.Instance.GetHashCode(obj.Line);
    }
}

