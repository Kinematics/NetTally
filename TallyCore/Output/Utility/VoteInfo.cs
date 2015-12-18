using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Output
{
    public static class VoteInfo
    {
        /// <summary>
        /// Get the URL for the post made by the specified voter.
        /// </summary>
        /// <param name="voter">The voter to look up.</param>
        /// <param name="quest">The quest being checked.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns the permalink URL for the voter.  Returns an empty string if not found.</returns>
        public static string GetVoterUrl(string voter, IQuest quest, VoteType voteType)
        {
            Dictionary<string, string> voters = VoteCounter.Instance.GetVotersCollection(voteType);

            string voteID;
            if (voters.TryGetValue(voter, out voteID))
                return quest.ForumAdapter.GetPermalinkForId(voteID);

            return string.Empty;
        }

    }
}
