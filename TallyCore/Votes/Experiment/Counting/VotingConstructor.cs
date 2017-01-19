using System;
using System.Collections.Generic;
using System.Threading;

namespace NetTally.Votes.Experiment
{
    // X marker can only contain other X
    // +/- marker can only contain other +/-
    // # rank marker can only contain X (entire block is given a single rank)
    // + score marker can contain X (copy parent score) or other + score


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
