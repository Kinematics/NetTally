using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.VoteCounting
{
    public class VotingRecords
    {
        public static HashSet<string> ReferenceVoters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, string> ReferenceVoterPosts { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Reset()
        {
            ReferenceVoters.Clear();
            ReferenceVoterPosts.Clear();

        }

        public static void AddVoter(string voterName, string postID)
        {
            ReferenceVoters.Add(voterName);
            ReferenceVoterPosts[voterName] = postID;

        }
    }
}
