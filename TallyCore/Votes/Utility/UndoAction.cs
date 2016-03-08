using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string Vote1 { get; }
        public string Vote2 { get; }
        public HashSet<string> Voters1 { get; }
        public HashSet<string> Voters2 { get; }
        public Dictionary<string, string> PostIDs { get; }

        public List<string> Voters { get; }
        public List<KeyValuePair<string, HashSet<string>>> PriorVotes { get; }

        // Delete
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            string vote1, HashSet<string> voters1)
        {
            if (actionType != UndoActionType.Delete)
                throw new InvalidOperationException("Invalid use of constructor for Delete undo.");
            if (vote1 == null)
                throw new ArgumentNullException(nameof(vote1));
            if (voters1 == null)
                throw new ArgumentNullException(nameof(voters1));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            Vote1 = vote1;
            Voters1 = new HashSet<string>(voters1 ?? Enumerable.Empty<string>());
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

        // Join
        public UndoAction(UndoActionType actionType, VoteType voteType, Dictionary<string, string> postIDs,
            List<string> voters, IEnumerable<KeyValuePair<string, HashSet<string>>> priorVotes)
        {
            if (actionType != UndoActionType.Join)
                throw new InvalidOperationException("Invalid use of constructor for Join undo.");
            if (voters == null)
                throw new ArgumentNullException(nameof(voters));
            if (priorVotes == null)
                throw new ArgumentNullException(nameof(priorVotes));

            ActionType = actionType;
            VoteType = voteType;
            PostIDs = new Dictionary<string, string>(postIDs, postIDs.Comparer);

            Voters = new List<string>(voters);

            PriorVotes = new List<KeyValuePair<string, HashSet<string>>>();
            foreach (var prior in priorVotes)
            {
                PriorVotes.Add(new KeyValuePair<string, HashSet<string>>(prior.Key, new HashSet<string>(prior.Value)));
            }
        }
    }
}
