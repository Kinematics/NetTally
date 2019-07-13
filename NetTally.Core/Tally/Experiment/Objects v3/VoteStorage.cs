using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Experiment3
{
    public class VoteStorage : Dictionary<VoteLineBlock, VoterStorage>
    {
    }


    public class VoterStorage : Dictionary<Origin, VoteLineBlock>
    {
    }
}
