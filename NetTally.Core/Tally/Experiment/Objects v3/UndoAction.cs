using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public enum UndoActionType
    {
        Merge,
        Split,
        Join,
        Delete,
        Other
    }

    public class UndoAction
    {
        public UndoActionType ActionType { get; }
        readonly VoteStorage storage;

        // Don't let Undo be called more than once.
        bool undone = false;

        public UndoAction(UndoActionType actionType, VoteStorage currentState,
            VoteLineBlock? storageVote = null)
        {
            ActionType = actionType;

            // Clone the current vote repository.
            storage = new VoteStorage(currentState);

            // Utilize a cloned storage vote if we need to track a changed VoteLineBlock 
            // that had internal properties changed.  Otherwise those will be propogated
            // to our storage collection.
            if (storageVote != null)
            {
                var storedVote = storageVote.Clone();
                storage.TryGetValue(storageVote, out var storedVoteSupporters);
                storage.Remove(storageVote);
                storage.Add(storedVote, storedVoteSupporters);
            }
        }

        public bool Undo(IVoteCounter voteCounter)
        {
            if (undone)
                return false;

            var currentVotes = voteCounter.VoteStorage;

            // Remove pass - Remove all current votes or supporters that are not
            // in the archived version of the vote repository.

            HashSet<VoteLineBlock> voteRemovals = new HashSet<VoteLineBlock>();

            foreach (var (currentVote, currentSupporters) in currentVotes)
            {
                if (!storage.TryGetValue(currentVote, out var storageSupporters))
                {
                    voteRemovals.Add(currentVote);
                }
                else
                {
                    HashSet<Origin> voterRemovals = new HashSet<Origin>();

                    foreach (var(currentSupporter, _) in currentSupporters)
                    {
                        if (!storageSupporters.ContainsKey(currentSupporter))
                        {
                            voterRemovals.Add(currentSupporter);
                        }
                    }

                    foreach (var removal in voterRemovals)
                    {
                        currentSupporters.Remove(removal);
                    }
                }
            }

            foreach (var removal in voteRemovals)
            {
                currentVotes.Remove(removal);
            }


            // Replace pass - Add all archived votes or supporters that are not
            // in the current version of the vote repository.

            foreach (var (storageVote, storageSupporters) in storage)
            {
                if (!currentVotes.TryGetValue(storageVote, out var currentSupporters))
                {
                    currentVotes.Add(storageVote, storageSupporters);
                }
                else
                {
                    foreach (var (storageSupporter, storageSupporterVote) in storageSupporters)
                    {
                        if (!currentSupporters.ContainsKey(storageSupporter))
                        {
                            currentSupporters.Add(storageSupporter, storageSupporterVote);
                        }
                    }
                }
            }

            return undone = true;
        }
    }
}
