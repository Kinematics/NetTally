using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public class Vote
    {
        public List<VoteLine> VoteLines { get; } = new List<VoteLine>();

        public List<List<VoteLine>> VoteBlocks
        {
            get
            {
                var a = from v in VoteLines
                        select new List<VoteLine>() { v };

                return a.ToList();
            }
        }
    }
}
