using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public interface ITextResultsProvider
    {
        string BuildOutput(IQuest quest, IVoteCounter voteCounter, DisplayMode displayMode);
    }
}
