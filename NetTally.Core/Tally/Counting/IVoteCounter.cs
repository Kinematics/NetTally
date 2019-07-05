using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Votes;
using NetTally.Experiment3;

namespace NetTally.VoteCounting
{
    public interface IVoteCounter : INotifyPropertyChanged
    {
        IQuest? Quest { get; set; }

        List<Experiment3.Post> PostsList { get; }
        void Reset();
        bool VoteCounterIsTallying { get; set; }
        bool TallyWasCanceled { get; set; }

        void AddVotes(IEnumerable<string> voteParts, string voter, string postID, VoteType voteType);

        bool Merge(string fromVote, string toVote, VoteType voteType);
        bool Join(List<string> voters, string voterToJoin, VoteType voteType);
        bool Delete(string vote, VoteType voteType);
        bool PartitionChildren(string vote, VoteType voteType, VoteConstructor constructor);

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
        bool HasNewerVote(Experiment3.Post post);

        HashSet<string> UserDefinedTasks { get; }
        List<string> OrderedTaskList { get; }
        IEnumerable<string> KnownTasks { get; }
        void ResetUserDefinedTasks(string forQuestName);
        void ResetUserMerges();

        HashSet<string> ReferenceVoters { get; }
        Dictionary<string, string> ReferenceVoterPosts { get; }
        HashSet<string> ReferencePlanNames { get; }
        Dictionary<string, List<string>> ReferencePlans { get; }
        Dictionary<string, List<VoteLine>> ReferencePlansEx3 { get; }

        bool AddReferencePlan(string planName, IEnumerable<string> planBlock, string postID);
        bool AddReferencePlan(string planName, IEnumerable<VoteLine> planBlock, string postID);
        string GetPlanPostId(string planName);

        bool AddReferenceVoter(string voterName, string postID);
        bool HasReferenceVoter(string voterName);
        string? GetReferenceVoter(string voterName);
        string? GetReferenceVoterPostId(string voterName);
        bool AddFutureReference(Post post);

        void AddVotes(IEnumerable<VoteLineBlock> voteParts, string voter, string postID, VoteType voteType);

        HashSet<Experiment3.Post> FutureReferences { get; }

        bool HasUndoActions { get; }
        bool Undo();
    }
}
