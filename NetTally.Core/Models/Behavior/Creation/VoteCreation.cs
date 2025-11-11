using NetTally.Tally.Parsing;
using NetTally.Tally.Processing;

namespace NetTally.Models;

public static class VoteCreation
{
    extension(Vote)
    {
        /// <summary>
        /// Create a new <see cref="Vote"/> object based on the provided <see cref="Post">.
        /// </summary>
        /// <param name="post">The post containing the potential vote.</param>
        /// <returns>A new <see cref="Vote"/> if one exists. Otherwise <c>null</c>.</returns>
        public static Vote? Create(Post? post)
        {
            if (post is null)
                return null;

            var voteLines = VoteParser.ExtractVoteLines(post.Text);

            if (voteLines.Count == 0)
                return null;

            return new Vote(post.Origin, [.. voteLines]);
        }

        /// <summary>
        /// Create a new <see cref="Vote"/> object based on the provided <see cref="Origin">
        /// and vote lines.
        /// </summary>
        /// <param name="origin">The <see cref="Origin"/> the vote is based on.</param>
        /// <param name="voteLines">The vote lines that go into the vote.</param>
        /// <returns>A new <see cref="Vote"/> if one exists. Otherwise <c>null</c>.</returns>
        public static Vote? Create(Origin? origin, List<VoteLine>? voteLines)
        {
            if (origin is null or NoOrigin ||
                voteLines is null ||
                voteLines.Count == 0)
                return null;

            return new Vote(origin, [.. voteLines]);
        }

        /// <summary>
        /// Create an encapsulation of a vote for processing.
        /// </summary>
        /// <param name="vote">The <see cref="Vote"/> to encapsulate.</param>
        /// <returns>A new <see cref="VoteToProcess"/>, or <c>null</c>.</returns>
        public static VoteToProcess? CreateToProcess(Vote? vote)
        {
            return vote is null ? null : new(vote);
        }
    }
}
