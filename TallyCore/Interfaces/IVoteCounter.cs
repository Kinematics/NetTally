using System.Collections.Generic;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IVoteCounter
    {
        string Title { get; set; }

        HashSet<string> PlanNames { get; }

        Dictionary<string, HashSet<string>> VotesWithSupporters { get; }
        Dictionary<string, string> VoterMessageId { get; }

        Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; }
        Dictionary<string, string> RankedVoterMessageId { get; }

        bool HasRankedVotes { get; }

        void TallyVotes(IQuest quest, List<HtmlDocument> pages);
        bool Merge(string fromVote, string toVote, VoteType voteType);
        bool Join(List<string> voters, string voterToJoin, VoteType voteType);
        bool Delete(string vote, VoteType voteType);
        bool Rename(string oldVote, string newVote, VoteType voteType);

        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
        Dictionary<string, string> GetVotersCollection(VoteType voteType);

        void Reset();

        void RemoveSupport(string voter, VoteType voteType);

        void AddVoterPostID(string voter, string postID, VoteType voteType);
        void AddVoteSupport(string vote, string voter, VoteType voteType, IQuest quest);

        List<string> GetVotesFromReference(string voteLine);
    }
}
