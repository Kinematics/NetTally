using System.Collections.Generic;
using System.Linq;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tally.ComponentsF.Storage;

public class MergeData
{
    public UndoActionType UndoActionType { get; }
    public VoteBlockType FromVote { get; }
    public VoteBlockType ToVote { get; }
    public List<VoteBlockType> ToVotes { get; }

    public MergeData(VoteBlockType fromVote, VoteBlockType toVote, UndoActionType actionType)
    {
        FromVote = fromVote;
        ToVote = toVote;
        ToVotes = [];
        UndoActionType = actionType;
    }

    public MergeData(VoteBlockType fromVote, IEnumerable<VoteBlockType> toVotes, UndoActionType actionType)
    {
        FromVote = fromVote;
        ToVote = fromVote;
        ToVotes = toVotes.ToList();
        UndoActionType = actionType;
    }
}

/// <summary>
/// A class to keep a record of all merges made for a given quest.
/// These merges can then be used to adjust the vote during re-tallies.
/// </summary>
public class MergeRecords
{
    readonly Dictionary<PartitionMode, List<MergeData>> MergeLookup = [];

    /// <summary>
    /// Gets the list of merges for the specified partition mode.
    /// Creates a new list if necessary.
    /// </summary>
    /// <param name="partitionMode">The partition mode.</param>
    /// <returns>Returns the list of merges stored for the specifed partition mode.</returns>
    private List<MergeData> GetMergesFor(PartitionMode partitionMode)
    {
        if (!MergeLookup.TryGetValue(partitionMode, out var merges))
        {
            merges = [];
            MergeLookup.Add(partitionMode, merges);
        }

        return merges;
    }

    /// <summary>
    /// Adds a merge record.
    /// </summary>
    /// <param name="fromRecord">The original vote string.</param>
    /// <param name="toRecord">The revised vote string.</param>
    /// <param name="partitionMode">The partition mode.</param>
    public void AddMergeRecord(VoteBlockType fromRecord, VoteBlockType toRecord,
        UndoActionType actionType, PartitionMode partitionMode)
    {
        var merges = GetMergesFor(partitionMode);

        MergeData data = new MergeData(fromRecord, toRecord, actionType);

        merges.Add(data);
    }

    /// <summary>
    /// Adds a merge record.
    /// </summary>
    /// <param name="fromRecord">The original vote string.</param>
    /// <param name="toRecord">The revised vote string.</param>
    /// <param name="partitionMode">The partition mode.</param>
    public void AddMergeRecord(VoteBlockType fromRecord, IEnumerable<VoteBlockType> toRecords,
        UndoActionType actionType, PartitionMode partitionMode)
    {
        var merges = GetMergesFor(partitionMode);

        MergeData data = new(fromRecord, toRecords, actionType);

        merges.Add(data);
    }

    /// <summary>
    /// Removes the most recently added merge record of the given partition mode.
    /// </summary>
    /// <param name="partitionMode">The partition mode.</param>
    public void RemoveLastMergeRecord(PartitionMode partitionMode, UndoActionType actionType)
    {
        var merges = GetMergesFor(partitionMode);

        if (merges.Count > 0)
        {
            int index = merges.FindLastIndex(a => a.UndoActionType == actionType);

            if (index >= 0)
                merges.RemoveAt(index);
        }
    }

    /// <summary>
    /// Tries the get merge record for the provided vote.
    /// Will recurse through votes til it finds the last one that was modified.
    /// </summary>
    /// <param name="original">The original vote string to check for.</param>
    /// <param name="partitionMode">The partition mode.</param>
    /// <param name="result">The result of the lookup.</param>
    /// <returns>Returns true if the record was found, and false if not.</returns>
    public IReadOnlyList<MergeData> GetMergeRecordList(PartitionMode partitionMode)
    {
        var merges = GetMergesFor(partitionMode);

        return merges;
    }

    /// <summary>
    /// Resets this instance.
    /// </summary>
    public void Reset()
    {
        foreach (var lookup in MergeLookup)
        {
            lookup.Value.Clear();
        }

        MergeLookup.Clear();
    }
}
