using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public class PostLine
    {
        public string Original { get; }
        public string Clean { get; }

        public PostLine(string line)
        {
            Original = line;
            Clean = VoteString.CleanVote(line);
        }
    }
}
