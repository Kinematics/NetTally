using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// A class to keep a record of all merges made for a given quest.
    /// These merges can then be used to adjust the vote during re-tallies.
    /// </summary>
    public class MergeRecords
    {
        readonly Dictionary<PartitionMode, Dictionary<string, string>> MergeLookup = new Dictionary<PartitionMode, Dictionary<string, string>>();

        /// <summary>
        /// Adds a merge record.
        /// </summary>
        /// <param name="original">The original vote string.</param>
        /// <param name="revised">The revised vote string.</param>
        /// <param name="partitionMode">The partition mode.</param>
        /// <exception cref="System.ArgumentNullException">
        /// original
        /// or
        /// merge
        /// </exception>
        public void AddMergeRecord(string original, string revised, PartitionMode partitionMode)
        {
            if (string.IsNullOrEmpty(original))
                throw new ArgumentNullException(nameof(original));
            if (string.IsNullOrEmpty(revised))
                throw new ArgumentNullException(nameof(revised));

            var merges = GetMergesFor(partitionMode);

            merges[original] = revised;
        }

        /// <summary>
        /// Removes the merge record.
        /// </summary>
        /// <param name="original">The original vote string.</param>
        /// <param name="revised">The revised vote string.</param>
        /// <param name="partitionMode">The partition mode.</param>
        /// <exception cref="System.ArgumentNullException">
        /// original
        /// or
        /// revised
        /// </exception>
        public void RemoveMergeRecord(string original, string revised, PartitionMode partitionMode)
        {
            if (string.IsNullOrEmpty(original))
                throw new ArgumentNullException(nameof(original));
            if (string.IsNullOrEmpty(revised))
                throw new ArgumentNullException(nameof(revised));

            var merges = GetMergesFor(partitionMode);

            if (merges[original] == revised)
            {
                merges.Remove(original);
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
        public bool TryGetMergeRecord(string original, PartitionMode partitionMode, out string result)
        {
            result = "";

            var merges = GetMergesFor(partitionMode);

            while (merges.TryGetValue(original, out string lookupResult))
            {
                result = lookupResult;
                original = lookupResult;
            }

            return (!string.IsNullOrEmpty(result));
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

        /// <summary>
        /// Gets the merges for the specified partition mode.
        /// </summary>
        /// <param name="partitionMode">The partition mode.</param>
        /// <returns>Returns the lookup dictionary for the specifed partition mode.</returns>
        private Dictionary<string, string> GetMergesFor(PartitionMode partitionMode)
        {
            if (!MergeLookup.TryGetValue(partitionMode, out Dictionary<string, string> merges))
            {
                merges = new Dictionary<string, string>();
                MergeLookup.Add(partitionMode, merges);
            }

            return merges;
        }
    }
}
