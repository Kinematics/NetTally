using System.Collections.Generic;
using NetTally.Votes;
using NetTally.Types.Enums;

namespace NetTally.VoteCounting
{
    public class MergeData
    {
        public UndoActionType UndoActionType { get; }
        public VoteLineBlock FromVote { get; }
        public VoteLineBlock ToVote { get; }
        public List<VoteLineBlock> ToVotes { get; } = new List<VoteLineBlock>();

        public MergeData(VoteLineBlock fromVote, VoteLineBlock toVote, UndoActionType actionType)
        {
            FromVote = fromVote;
            ToVote = toVote;
            UndoActionType = actionType;
        }

        public MergeData(VoteLineBlock fromVote, List<VoteLineBlock> toVotes, UndoActionType actionType)
        {
            FromVote = fromVote;
            ToVote = fromVote;
            ToVotes.AddRange(toVotes);
            UndoActionType = actionType;
        }
    }

    /// <summary>
    /// A class to keep a record of all merges made for a given quest.
    /// These merges can then be used to adjust the vote during re-tallies.
    /// </summary>
    public class MergeRecords
    {
        readonly Dictionary<PartitionMode, List<MergeData>> MergeLookup
            = new Dictionary<PartitionMode, List<MergeData>>();

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
                merges = new List<MergeData>();
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
        public void AddMergeRecord(VoteLineBlock fromRecord, VoteLineBlock toRecord,
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
        public void AddMergeRecord(VoteLineBlock fromRecord, List<VoteLineBlock> toRecords,
            UndoActionType actionType, PartitionMode partitionMode)
        {
            var merges = GetMergesFor(partitionMode);

            MergeData data = new MergeData(fromRecord, toRecords, actionType);

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
}
