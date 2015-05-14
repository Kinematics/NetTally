using System.Collections.Generic;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IVoteCounter
    {
        string Title { get; set; }
        Dictionary<string, HashSet<string>> VotesWithSupporters { get; }
        Dictionary<string, string> VoterMessageId { get; }
        HashSet<string> PlanNames { get; }

        void TallyVotes(IQuest quest, List<HtmlDocument> pages);
    }
}
