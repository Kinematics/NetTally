using System.Collections.Generic;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IVoteCounter
    {
        Dictionary<string, HashSet<string>> VotesWithSupporters { get; }
        Dictionary<string, string> VoterMessageId { get; }

        bool UseVotePartitions { get; set; }
        bool PartitionByLine { get; set; }


        void TallyVotes(List<HtmlDocument> pages, int startPost, int endPost);
    }
}
