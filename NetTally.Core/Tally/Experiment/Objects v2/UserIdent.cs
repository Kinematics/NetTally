using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class UserIdent : Identity
    {
        public UserIdent(string name)
            :base(name, false, 0)
        {

        }
    }
}
