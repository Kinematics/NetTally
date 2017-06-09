using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    public interface IVoteCounter : INotifyPropertyChanged
    {
        IQuest Quest { get; set; }

        Task TallyPosts(CancellationToken token);
        Task TallyPosts(IEnumerable<PostComponents> posts, IQuest quest, CancellationToken token);

        List<PostComponents> PostsList { get; }
        void Reset();
        bool VoteCounterIsTallying { get; }
        bool TallyWasCanceled { get; }

        void AddVotes(IEnumerable<string> voteParts, string voter, string postID, VoteType voteType);

        bool Merge(string fromVote, string toVote, VoteType voteType);
        bool Join(List<string> voters, string voterToJoin, VoteType voteType);
        bool Delete(string vote, VoteType voteType);

        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
        Dictionary<string, string> GetVotersCollection(VoteType voteType);

        List<string> GetCondensedRankVotes();
        List<string> GetVotesFromReference(string voteLine, string author);

        string Title { get; set; }
        HashSet<string> PlanNames { get; }

        bool HasRankedVotes { get; }
        bool HasPlan(string planName);
        bool HasVote(string vote, VoteType voteType);
        bool HasVoter(string voterName, VoteType voteType);
        bool HasUserEnteredVoter(string voterName, VoteType voteType);
        bool HasNewerVote(PostComponents post);

        HashSet<string> UserDefinedTasks { get; }
        void ResetUserDefinedTasks(string forQuestName);
        List<string> OrderedTaskList { get; }

        HashSet<string> ReferenceVoters { get; }
        Dictionary<string, string> ReferenceVoterPosts { get; }
        HashSet<string> ReferencePlanNames { get; }
        Dictionary<string, List<string>> ReferencePlans { get; }

        HashSet<PostComponents> FutureReferences { get; }

        bool HasUndoActions { get; }

        bool Undo();
    }
}
