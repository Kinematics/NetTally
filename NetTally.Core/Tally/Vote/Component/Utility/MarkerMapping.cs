namespace NetTally.Tally.Vote.Component;

/// <summary>
/// Extension class containing mapping function for subclasses of <see cref="Marker"/> objects.
/// </summary>
public static class MarkerMapping
{
    extension(Marker marker)
    {
        /// <summary>
        /// Map function that defines how to implement a function that can apply to different
        /// subclasses of <see cref="Marker"/>.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="origin">The <see cref="Origin"/> that this extension method applies to.</param>
        /// <param name="voteMap">What to do if the <see cref="Marker"/> is a <see cref="VoteMarker"/></param>
        /// <param name="rankMap">What to do if the <see cref="Marker"/> is a <see cref="RankMarker"/></param>
        /// <param name="scoreMap">What to do if the <see cref="Marker"/> is a <see cref="ScoreMarker"/></param>
        /// <param name="approvalMap">What to do if the <see cref="Marker"/> is a <see cref="ApprovalMarker"/></param>
        /// <param name="planMap">What to do if the <see cref="Marker"/> is a <see cref="PlanMarker"/></param>
        /// <param name="emptyMap">What to do if the <see cref="Marker"/> is a <see cref="NoMarker"/></param>
        /// <returns>The result of whichever function got applied.</returns>
        /// <exception cref="InvalidOperationException">Will trigger if another subclass is
        /// ever created, but this function hasn't been updated.</exception>
        public T Map<T>(
            Func<VoteMarker, T> voteMap,
            Func<RankMarker, T> rankMap,
            Func<ScoreMarker, T> scoreMap,
            Func<ApprovalMarker, T> approvalMap,
            Func<PlanMarker, T> planMap,
            Func<NoMarker, T> emptyMap)
        {
            return marker switch
            {
                VoteMarker voteMarker => voteMap(voteMarker),
                RankMarker rankMaker => rankMap(rankMaker),
                ScoreMarker scoreMarker => scoreMap(scoreMarker),
                ApprovalMarker approvalMarker => approvalMap(approvalMarker),
                PlanMarker planMarker => planMap(planMarker),
                NoMarker noMarker => emptyMap(noMarker),
                _ => throw new InvalidOperationException("Unknown Marker type.")
            };
        }
    }
}
