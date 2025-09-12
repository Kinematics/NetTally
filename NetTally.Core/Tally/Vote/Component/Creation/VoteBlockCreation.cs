using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;

namespace NetTally.Tally.Vote.Component.Creation;

/// <summary>
/// Extension class for the creation of <see cref="VoteBlock"/> objects.
/// </summary>
public static class VoteBlockCreation
{
    extension(VoteBlock)
    {
        public static VoteBlock Empty => _empty;

        /// <summary>
        /// Create a <see cref="VoteBlock"> with the given <see cref="VoteLine">s.
        /// </summary>
        /// <param name="lines">The vote lines to add to the vote block.</param>
        /// <returns>A new <see cref="VoteBlock"/></returns>
        public static VoteBlock Create(IEnumerable<VoteLine> lines)
        {
            List<VoteLine> listOfLines = [.. lines];

            if (listOfLines.Count == 0)
            {
                return VoteBlock.Empty;
            }

            return new VoteBlock([.. listOfLines],
                                     listOfLines[0].Marker,
                                     listOfLines[0].Task);
        }

        /// <summary>
        /// Create a <see cref="VoteBlock"> with the given <see cref="VoteLine">.
        /// </summary>
        /// <param name="line">The vote line to add to the vote block.</param>
        /// <returns>A new <see cref="VoteBlock"/></returns>
        public static VoteBlock Create(VoteLine line)
        {
            return new VoteBlock([line], line.Marker, line.Task);
        }

        /// <summary>
        /// Create a <see cref="VoteBlock"/> containing all the vote lines of
        /// the provided <see cref="VoteBlock"/>s.
        /// </summary>
        /// <param name="blocks">A collection of <see cref="VoteBlock"/>s that will
        /// be used as the source for this one.</param>
        /// <returns>A new <see cref="VoteBlock"/>.</returns>
        public static VoteBlock Create(IEnumerable<VoteBlock> blocks)
        {
            var lines = blocks.SelectMany(x => x.Lines);
            return Create(lines);
        }
    }

    extension (VoteBlock voteBlock)
    {
        /// <summary>
        /// Create a deep copy of the provided <see cref="VoteBlock"/>.
        /// </summary>
        /// <returns>A <see cref="VoteBlock"/> will all the same lines,
        /// marker, and task as the original.</returns>
        public VoteBlock Clone()
        {
            return new VoteBlock([.. voteBlock.Lines], voteBlock.Marker, voteBlock.Task);
        }
    }

    private static readonly VoteBlock _empty = new([], Marker.Empty, VoteTask.Empty);
}
