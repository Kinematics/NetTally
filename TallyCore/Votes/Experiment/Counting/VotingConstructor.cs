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
        public static HashSet<Post> FutureReferences { get; } = new HashSet<Post>();

        public static void Reset()
        {
            FutureReferences.Clear();
        }

        internal static List<string> GetWorkingVote(Post post)
        {
            throw new NotImplementedException();
        }

        internal static bool ProcessPost(Post post, IQuest quest, CancellationToken token)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!post.HasVote)
                throw new ArgumentException("Post is not a valid vote.");

            token.ThrowIfCancellationRequested();

            if (HasFutureReference(post))
            {
                FutureReferences.Add(post);
                return false;
            }



            throw new NotImplementedException();
        }

        private static bool HasFutureReference(Post post)
        {
            throw new NotImplementedException();
        }
    }
}
