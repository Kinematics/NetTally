namespace NetTally.Models;

public static class VoteUtility
{
    extension(Vote vote)
    {
        /// <summary>
        /// Determine if a post falls before the starting point of the tallied range.
        /// </summary>
        /// <param name="vote">The <see cref="Vote"> to check</param>
        /// <param name="threadRange">The range of posts examined in the thread.</param>
        /// <returns><c>True</c> if the post falls before the tally starting point.</returns>
        public bool IsBeforeStart(ThreadRange threadRange)
        {
            return threadRange switch
            {
                ThreadRangeByStartingId range => vote.Origin.PostId < range.StartingPostId,
                ThreadRangeByStartingPost range => vote.Origin.PostNumber < range.StartPostNumber,
                ThreadRangeByPostRange range => vote.Origin.PostNumber < range.StartPostNumber,
                _ => throw new NotImplementedException($"Unknown ThreadRange type: {threadRange.GetType()}")
            };
        }

        /// <summary>
        /// Determine if a post falls after the ending point of the tallied range.
        /// </summary>
        /// <param name="vote">The <see cref="Vote"> to check</param>
        /// <param name="quest">The quest being tallied</param>
        /// <param name="threadRange">The tally range</param>
        /// <returns><c>True</c> if the post falls after the tally ending point.</returns>
        public bool IsAfterEnd(ThreadRange threadRange)
        {
            return threadRange switch
            {
                ThreadRangeByStartingId => false,
                ThreadRangeByStartingPost => false,
                ThreadRangeByPostRange range => vote.Origin.PostNumber > range.EndPostNumber,
                _ => throw new InvalidOperationException("Unknown ThreadRange type.")
            };
        }

        /// <summary>
        /// Checks if a post matches a username filter in the given quest.
        /// </summary>
        /// <param name="vote">The <see cref="Vote"> to check</param>
        /// <param name="quest">The <see cref="Quest"/> with the filter.</param>
        /// <returns><c>True</c> if the username filter matches. Otherwise <c>false</c>.</returns>
        public bool MatchesUsernameFilter(Quest quest)
        {
            return quest.UseCustomUsernameFilters && quest.UsernameFilter.Blocks(vote.Origin.GetName().DisplayName);
        }

        /// <summary>
        /// Checks if a post matches a post number filter in the given quest.
        /// </summary>
        /// <param name="vote">The <see cref="Vote"> to check</param>
        /// <param name="quest">The <see cref="Quest"/> with the filter.</param>
        /// <returns><c>True</c> if the post number filter matches. Otherwise <c>false</c>.</returns>
        public bool MatchesPostNumberFilter(Quest quest)
        {
            return quest.UseCustomPostFilters &&
                (quest.PostsFilter.Blocks(vote.Origin.PostNumber.Value) ||
                 quest.PostsFilter.Blocks(vote.Origin.PostId.Value));
        }
    }
}
