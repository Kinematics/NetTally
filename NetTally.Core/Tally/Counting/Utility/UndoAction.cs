using System;
using System.Collections.Generic;

namespace NetTally.Votes.Original
{
    public enum UndoActionType
    {
        Merge,
        Join,
        Delete,
        PartitionChildren
    }

    public class UndoAction
    {
        public UndoActionType ActionType { get; }
        public VoteType VoteType { get; }

        public Dictionary<string, string> PostIDs { get; }

        public string Vote1 { get; } = "";
        public string Vote2 { get; } = "";
        public HashSet<string> Voters1 { get; } = new HashSet<string>();
        public HashSet<string> Voters2 { get; } = new HashSet<string>();

        public List<string> JoinedVoters { get; } = new List<string>();
        public List<KeyValuePair<string, HashSet<string>>> PriorVotes { get; } = new List<KeyValuePair<string, HashSet<string>>>();
        public Dictionary<KeyValuePair<string, HashSet<string>>, string> MergedVotes { get; } = new Dictionary<KeyValuePair<string, HashSet<string>>, string>();
        public Dictionary<string, HashSet<string>> DeletedVotes { get; } = new Dictionary<string, HashSet<string>>();


        // Delete votes
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            Dictionary<string, HashSet<string>> deletedVotes)
        {
            if (actionType != UndoActionType.Delete)
                throw new InvalidOperationException("Invalid use of constructor for Delete undo.");
            if (deletedVotes == null)
                throw new ArgumentNullException(nameof(deletedVotes));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            foreach (var deletedVote in deletedVotes)
            {
                DeletedVotes.Add(deletedVote.Key, new HashSet<string>(deletedVote.Value));
            }
        }

        // Merge
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            string vote1, HashSet<string> voters1,
            string vote2, HashSet<string> voters2)
        {
            if (actionType != UndoActionType.Merge)
                throw new InvalidOperationException("Invalid use of constructor for Merge undo.");

            Vote1 = vote1;
            Vote2 = vote2;

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            Voters1.UnionWith(voters1);
            Voters2.UnionWith(voters2);
        }

        // Merge rank votes
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            Dictionary<KeyValuePair<string, HashSet<string>>, string> mergedVotes)
        {
            if (actionType != UndoActionType.Merge)
                throw new InvalidOperationException("Invalid use of constructor for Merge undo.");
            if (voteType != VoteType.Rank)
                throw new InvalidOperationException("Invalid use of constructor for non-rank merge.");
            if (postIDs == null)
                throw new ArgumentNullException(nameof(postIDs));
            if (mergedVotes == null)
                throw new ArgumentNullException(nameof(mergedVotes));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            foreach (var mergedVote in mergedVotes)
            {
                // original vote, revised vote key
                MergedVotes.Add(new KeyValuePair<string, HashSet<string>>(mergedVote.Key.Key, new HashSet<string>(mergedVote.Key.Value)), mergedVote.Value);
            }
        }

        // Join
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            List<string> joinedVoters, IEnumerable<KeyValuePair<string, HashSet<string>>> priorVotes)
        {
            if (actionType != UndoActionType.Join)
                throw new InvalidOperationException("Invalid use of constructor for Join undo.");
            if (joinedVoters == null)
                throw new ArgumentNullException(nameof(joinedVoters));
            if (priorVotes == null)
                throw new ArgumentNullException(nameof(priorVotes));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            JoinedVoters.AddRange(joinedVoters);

            foreach (var prior in priorVotes)
            {
                PriorVotes.Add(new KeyValuePair<string, HashSet<string>>(prior.Key, new HashSet<string>(prior.Value)));
            }
        }

        // Partition Children votes
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            HashSet<string> addedVotes, string deletedVote, HashSet<string> voters)
        {
            if (actionType != UndoActionType.PartitionChildren)
                throw new InvalidOperationException("Invalid use of constructor for Partition undo.");
            if (addedVotes == null)
                throw new ArgumentNullException(nameof(addedVotes));
            if (voters == null)
                throw new ArgumentNullException(nameof(voters));
            if (string.IsNullOrEmpty(deletedVote))
                throw new ArgumentNullException(nameof(deletedVote));


            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            Vote1 = deletedVote;

            Voters1.UnionWith(voters);
            Voters2.UnionWith(addedVotes);

        }

    }
}
