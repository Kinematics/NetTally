using System;
using System.Collections.Generic;
using System.Threading;

namespace NetTally.Votes.Experiment
{
    public static class VotingConstructor
    {
        internal static List<string> GetWorkingVote(Post post)
        {
            throw new NotImplementedException();
        }

        internal static bool ProcessPost(Post post, IQuest currentQuest, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            throw new NotImplementedException();
        }
    }
}
