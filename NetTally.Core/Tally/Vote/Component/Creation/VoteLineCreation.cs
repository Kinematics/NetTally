namespace NetTally.Tally.Vote.Component.Creation;

/// <summary>
/// Extension class for creating <see cref="VoteLine"/> objects.
/// </summary>
public static class VoteLineCreation
{
    extension(VoteLine)
    {
        /// <summary>
        /// The default empty <see cref="VoteLine"/>.
        /// </summary>
        public static VoteLine Empty => _empty;

        /// <summary>
        /// Create a new <see cref="VoteLine"/> using the provided components.
        /// Marker and Content must be non-empty.
        /// </summary>
        /// <param name="prefix">The prefix component of the vote line.</param>
        /// <param name="marker">The marker of the vote line.</param>
        /// <param name="task">The task of the vote line.</param>
        /// <param name="content">The content of the vote line.</param>
        /// <returns>A new <see cref="VoteLine"/></returns>
        public static VoteLine Create(
            Prefix prefix,
            Marker marker,
            VoteTask task,
            VoteContent content)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            ArgumentNullException.ThrowIfNull(marker);
            ArgumentNullException.ThrowIfNull(task);
            ArgumentNullException.ThrowIfNull(content);

            if (content == VoteContent.Empty)
                return VoteLine.Empty;

            return new(prefix, marker, task, content);
        }
    }

    private static readonly VoteLine _empty = 
        new (Prefix.Empty, Marker.Empty, VoteTask.Empty, VoteContent.Empty);
}
