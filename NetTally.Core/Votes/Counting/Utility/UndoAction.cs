using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Votes
{
    public enum UndoActionType
    {
        Merge,
        Join,
        Delete
    }

    public class UndoAction
    {
        public UndoActionType ActionType { get; }
        public VoteType VoteType { get; }

        public Dictionary<string, string> PostIDs { get; }

        public string Vote1 { get; }
        public string Vote2 { get; }
        public HashSet<string> Voters1 { get; }
        public HashSet<string> Voters2 { get; }

        public List<string> JoinedVoters { get; }
        public List<KeyValuePair<string, HashSet<string>>> PriorVotes { get; }
        public Dictionary<KeyValuePair<string, HashSet<string>>, string> MergedVotes { get; }
        public Dictionary<string, HashSet<string>> DeletedVotes { get; }


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

            DeletedVotes = new Dictionary<string, HashSet<string>>();
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
            if (vote1 == null)
                throw new ArgumentNullException(nameof(vote1));
            if (voters1 == null)
                throw new ArgumentNullException(nameof(voters1));
            if (vote2 == null)
                throw new ArgumentNullException(nameof(vote2));
            if (voters2 == null)
                throw new ArgumentNullException(nameof(voters2));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            Vote1 = vote1;
            Voters1 = new HashSet<string>(voters1 ?? Enumerable.Empty<string>());

            Vote2 = vote2;
            Voters2 = new HashSet<string>(voters2 ?? Enumerable.Empty<string>());
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

            MergedVotes = new Dictionary<KeyValuePair<string, HashSet<string>>, string>();
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

            JoinedVoters = new List<string>(joinedVoters);

            PriorVotes = new List<KeyValuePair<string, HashSet<string>>>();
            foreach (var prior in priorVotes)
            {
                PriorVotes.Add(new KeyValuePair<string, HashSet<string>>(prior.Key, new HashSet<string>(prior.Value)));
            }
        }
    }
}
