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

        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
    }
}
