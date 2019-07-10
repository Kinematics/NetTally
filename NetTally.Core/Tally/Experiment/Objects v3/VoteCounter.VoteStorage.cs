using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Utility;
using NetTally.Votes;
using NetTally.Experiment3;
using NetTally.Extensions;

namespace NetTally.VoteCounting.Experiment3
{
	/// <summary>
    /// Readonly interface access to vote storage.
    /// </summary>
    public interface IVoteStorageReader
    {
        IEnumerable<VoteRecord> Votes { get; }
    }

    public class VoteRecord
    {
        Dictionary<string, VoteLineBlock> supportEntries = new Dictionary<string, VoteLineBlock>();
    }

    // Intend to expand this as a partial class, to isolate the code.
    public class VoteCounter
    {
		/// <summary>
        /// Read-write interface access to vote storage.
        /// Not externally accessible.
        /// </summary>
        private interface IVoteStorageReaderWriter
        {
            void Clear();
        }

		/// <summary>
        /// An implementation for vote storage internal to VoteCounter.
        /// </summary>
        public class VoteStorage : IVoteStorageReader, IVoteStorageReaderWriter
        {
            public void Clear() { }
            public IEnumerable<VoteRecord> Votes { get; }
        }

        // Local vote storage.
        readonly VoteStorage voteStorage = new VoteStorage();

		/// <summary>
        /// Get readonly access to vote storage.
        /// </summary>
        public IVoteStorageReader GetVoteStorage => voteStorage;


    }
}
