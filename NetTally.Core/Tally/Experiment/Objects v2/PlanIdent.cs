using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class PlanIdent : Identity
    {
        public PlanIdent(string name, int number = 0)
            :base(name, true, number)
        {

        }
    }
}
