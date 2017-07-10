using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class VoteRecords
    {
        public IQuest Quest { get; set; }

        public List<Post> Posts { get; } = new List<Post>();
        public HashSet<Identity> Voters { get; } = new HashSet<Identity>();

        public void Reset()
        {
            Voters.Clear();
        }

        public void Initialize(IQuest quest, IEnumerable<Post> posts)
        {
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));
            
            Quest = quest ?? throw new ArgumentNullException(nameof(quest));
            Posts.Clear();
            Posts.AddRange(posts);
        }

        public void AddVoters(IEnumerable<Identity> voters)
        {
            Voters.UnionWith(voters);
        }
    }
}
