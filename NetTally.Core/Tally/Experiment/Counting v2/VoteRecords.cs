using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class VoteRecords
    {
        public IQuest? Quest { get; set; }

        public List<Post> Posts { get; } = new List<Post>();

        public void UsePosts(IEnumerable<Post> posts)
        {
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));
            
            Posts.Clear();
            Posts.AddRange(posts.Where(p => p.VoteContent.Valid));
        }

        public void Reset()
        {

        }
    }
}
