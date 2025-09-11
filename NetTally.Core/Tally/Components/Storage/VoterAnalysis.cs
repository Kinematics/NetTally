using NetTally.Enums;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.RankCounting.Reference;
using NetTally.Tally.Components.Votes;

namespace NetTally.Tally.Components.Storage;
public static class VoterAnalysis
{
    #region Queries - Counts
    /// <summary>
    /// Get the total number of users who are vote supporters.
    /// </summary>
    /// <returns>The number of users in storage.</returns>
    public static int GetUserCount(VoterStorageType storage)
    {
        return storage.Count(s => s.Key is UserOrigin);
    }

    /// <summary>
    /// Get the total number of users supporting the vote who
    /// used standard, score, or approval votes.
    /// </summary>
    /// <returns>The number of users making non-rank votes.</returns>
    public static int GetNonRankUserCount(VoterStorageType storage)
    {
        return GetNonRankUsers(storage).Count();
    }

    /// <summary>
    /// Get the total number of positively supporting users in this vote.
    /// Approval +'s and Scores above 50 count for support.
    /// </summary>
    /// <returns>The number of users who expressed positive support.</returns>
    public static int GetSupportCount(VoterStorageType storage)
    {
        return storage.Count(s => s.Key is UserOrigin && s.Value.Marker.IsPositive.GetValueOrDefault());
    }

    /// <summary>
    /// Gets the overall score for this vote.
    /// </summary>
    /// <returns>Returns a triplet of the score (int rounded version of the average),
    /// the average, and the lower 95% statistical margin.</returns>
    public static (int score, double average, double lowerMargin) GetScore(VoterStorageType storage)
    {
        var users = GetNonRankUsers(storage);

        int count = 0;
        int accum = 0;

        var (rating, lowerBound) = RankingCalculations.GetLowerWilsonScore(users, a => a.Value.Marker.Value);

        foreach (var (userOrigin, userVote) in users)
        {
            count++;
            accum += userVote.Marker.Value;
        }

        if (count == 0)
            return (0, 0, 0);

        double average = (double)accum / count;
        int simpleScore = (int)Math.Round(average, 0, MidpointRounding.AwayFromZero);

        return (simpleScore, average, lowerBound);
    }

    /// <summary>
    /// Get the overall approval for this vote.
    /// </summary>
    /// <returns>Returns the positive and negative results of how
    /// users voted for this vote.  A value above 50 is positive,
    /// while 50 and lower is negative.</returns>
    public static (int positive, int negative) GetApproval(VoterStorageType storage)
    {
        var users = GetNonRankUsers(storage);

        int positive = 0;
        int negative = 0;

        // Standard votes have a value of 100, Approval+ have a value of 80, and Scores are variable.
        // Sum up the positive and negative results.
        foreach (var (userOrigin, userVote) in users)
        {
            if (userVote.Marker.IsPositive.GetValueOrDefault())
                positive++;
            else
                negative++;
        }

        return (positive, negative);
    }
    #endregion Queries - Counts

    #region Queries - Ordered Results
    /// <summary>
    /// Gets an ordered version of the provided voters.
    /// The first voter was the first voter to support the vote, and
    /// the rest of the voters are alphabatized.
    /// </summary>
    /// <param name="voters">The voters being ordered.</param>
    /// <returns>Returns an ordered list of the voters.</returns>
    public static OrderedVoterStorageF GetOrderedVoterList(VoterStorageType storage)
    {
        // If 0 or 1 voters, nothing to sort
        if (storage.Count() < 2)
        {
            return [.. storage];
        }

        VoterStorageEntryF? firstEntry = GetFirstVoter(storage);

        if (firstEntry == null)
            return [];

        var orderRemaining = storage
            .Where(v => !OriginComparer.Instance.Equals(v.Key, firstEntry.Value.Key))
            .OrderByDescending(v => v.Value.Marker.Value)
            .ThenBy(v => v.Key, OriginComparer.Instance);

        OrderedVoterStorageF voterList = [firstEntry.Value, .. orderRemaining];

        return voterList;
    }

    /// <summary>
    /// Gets an ordered version of the provided voters.
    /// The list is ordered by the rank value each voter used, then alphabetically.
    /// Any non-rank votes are added at the end.
    /// </summary>
    /// <param name="voters">The voters being ordered.</param>
    /// <returns>Returns an ordered list of the voters.</returns>
    public static OrderedVoterStorageF GetOrderedRankedVoterList(VoterStorageType storage)
    {
        var ranksOnly = storage
            .Where(v => v.Value.Marker is RankMarker)
            .OrderBy(v => v.Value.Marker.Value)
            .ThenBy(v => v.Key, OriginComparer.Instance);
        var others = storage
            .Where(v => v.Value.Marker is not RankMarker)
            .OrderBy(v => v.Key, OriginComparer.Instance);

        OrderedVoterStorageF result = [.. ranksOnly, .. others];

        return result;
    }
    #endregion Queries - Ordered Results

    #region Queries - General
    static readonly List<MarkerType> nonRankMarkerTypes = [MarkerType.Vote, MarkerType.Score, MarkerType.Approval];

    /// <summary>
    /// Get users from storage that used non-rank voting.
    /// </summary>
    /// <returns></returns>
    public static VoterStorageType GetNonRankUsers(VoterStorageType storage)
    {
        return storage.Where(s => s.Key.IsUser &&
                               nonRankMarkerTypes.Contains(s.Value.Marker.Type));
    }
    #endregion

    #region Support functions
    /// <summary>
    /// Get the first voter from the provided list of VoterStorage entries.
    /// Plans always have priority over users.
    /// </summary>
    /// <param name="voters">The VoterStorage collection of voters.</param>
    /// <returns>Returns the earliest VoterStorageEntry found.</returns>
    //private (OriginType voter, VoteBlockType vote) GetFirstVoter()
    private static VoterStorageEntryF? GetFirstVoter(VoterStorageType storage)
    {
        if (!storage.Any())
            return null;

        var entries = storage.Where(v => v.Key is PlanOrigin);

        if (!entries.Any())
        {
            entries = storage;
        }

        var sorted = entries.OrderBy(v => v.Key.PostId, PostIdComparer.Instance);

        return sorted.FirstOrDefault();
    }
    #endregion Support functions

}
