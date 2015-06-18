using System.Collections.Generic;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IVoteCounter
    {
        void TallyVotes(IQuest quest, List<HtmlDocument> pages);
        void Reset();
        void AddVoteSupport(string vote, string voter, VoteType voteType, IQuest quest);
        void RemoveSupport(string voter, VoteType voteType);
        void AddVoterPostID(string voter, string postID, VoteType voteType);
        bool Merge(string fromVote, string toVote, VoteType voteType);
        bool Join(List<string> voters, string voterToJoin, VoteType voteType);
        bool Rename(string oldVote, string newVote, VoteType voteType);
        bool Delete(string vote, VoteType voteType);

        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
        Dictionary<string, string> GetVotersCollection(VoteType voteType);

        List<string> GetVotesFromReference(string voteLine);

        string Title { get; set; }
        bool HasRankedVotes { get; }

        Dictionary<string, HashSet<string>> VotesWithSupporters { get; }
        Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; }
        Dictionary<string, string> VoterMessageId { get; }
        Dictionary<string, string> RankedVoterMessageId { get; }

        HashSet<string> PlanNames { get; }

        List<PostComponents> VotePosts { get; }
        List<PostComponents> FloatingReferences { get; }
        bool HoldFloatingReferences { get; }
    }
}
