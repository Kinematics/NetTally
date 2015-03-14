using System.Collections.Generic;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IVoteCounter
    {
        Dictionary<string, HashSet<string>> VotesWithSupporters { get; }
        Dictionary<string, string> VoterMessageId { get; }

        void TallyVotes(IQuest quest, List<HtmlDocument> pages);
    }
}
