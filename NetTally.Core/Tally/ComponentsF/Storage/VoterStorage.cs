using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tally.ComponentsF.Storage;

/// <summary>
/// Used in conjunction with <seealso cref="VoteStorage"/>, for
/// keeping track of voters and their associated votes.
/// VoterStorage is a dictionary of voter origins and the vote
/// each submitted.
/// </summary>
public class VoterStorage : Dictionary<OriginType, VoteBlockType>
{
    #region Constructors
    /// <summary>
    /// Default constructor
    /// </summary>
    public VoterStorage()
        : base(OriginComparer.Instance)
    { }

    /// <summary>
    /// Constructor that takes a provided collection of voter storage elements.
    /// </summary>
    /// <param name="collectedVoterStorage">Voter storage elements to initialize
    /// the <see cref="VoterStorage"/> object with.</param>
    public VoterStorage(CollectedVoterStorageF collectedVoterStorage)
        : base(collectedVoterStorage, OriginComparer.Instance)
    { }
    #endregion Constructors

    #region Queries - Has XX?
    /// <summary>
    /// Check whether a given origin matches any voters stored in this lookup.
    /// </summary>
    /// <param name="origin">The origin to compare to.</param>
    /// <returns>Returns true if the origin exists in this lookup.</returns>
    public bool HasIdentity(OriginType origin)
    {
        return ContainsKey(origin);
    }

    /// <summary>
    /// Check whether a given plan name exists in the voters stored in this lookup.
    /// </summary>
    /// <param name="planName">The name of the plan to check for.</param>
    /// <returns>Returns true if the plan name can be found in this lookup.</returns>
    public bool HasPlan(string planName)
    {
        var author = Author.Create(planName);
        var origin = Origin.CreateOriginForName(IdentityType.Plan, author);
        return origin != null && ContainsKey(origin);
    }

    /// <summary>
    /// Check whether a given voter name exists in the voters stored in this lookup.
    /// </summary>
    /// <param name="voterName">The name of the voter to check for.</param>
    /// <returns>Returns true if the voter name can be found in this lookup.</returns>
    public bool HasVoter(string voterName)
    {
        var author = Author.Create(voterName);
        var origin = Origin.CreateOriginForName(IdentityType.User, author);
        return origin != null && ContainsKey(origin);
    }
    #endregion Queries - Has XX?

    #region Queries - Counts
    /// <summary>
    /// Get the total number of users who are vote supporters.
    /// </summary>
    /// <returns>The number of users in storage.</returns>
    public int GetUserCount()
    {
        return this.Count(s => s.Key.IsUser);
    }

    /// <summary>
    /// Get the total number of users supporting the vote who
    /// used standard, score, or approval votes.
    /// </summary>
    /// <returns>The number of users making non-rank votes.</returns>
    public int GetNonRankUserCount()
    {
        return GetNonRankUsers().Count();
    }

    /// <summary>
    /// Get the total number of positively supporting users in this vote.
    /// Approval +'s and Scores above 50 count for support.
    /// </summary>
    /// <returns>The number of users who expressed positive support.</returns>
    public int GetSupportCount()
    {
        return this.Count(s => s.Key.IsUser &&
                          MarkerComparer.IsPositive(s.Value.Marker).GetValueOrDefault());
    }

    /// <summary>
    /// Gets the overall score for this vote.
    /// </summary>
    /// <returns>Returns a triplet of the score (int rounded version of the average),
    /// the average, and the lower 95% statistical margin.</returns>
    public (int score, double average, double lowerMargin) GetScore()
    {
        var users = GetNonRankUsers();

        int count = 0;
        int accum = 0;

        var (rating, lowerBound) = VoteCounting.RankVotes.Reference
            .RankingCalculations.GetLowerWilsonScore(users, a => a.Value.Marker.MarkerValue);

        foreach (var (userOrigin, userVote) in users)
        {
            count++;
            accum += userVote.Marker.MarkerValue;
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
    public (int positive, int negative) GetApproval()
    {
        var users = GetNonRankUsers();

        int positive = 0;
        int negative = 0;

        // Standard votes have a value of 100, Approval+ have a value of 80, and Scores are variable.
        // Sum up the positive and negative results.
        foreach (var (userOrigin, userVote) in users)
        {
            if (userVote.Marker.MarkerValue > 50)
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
    public OrderedVoterStorageF GetOrderedVoterList()
    {
        // If 0 or 1 voters, nothing to sort
        if (Count < 2)
        {
            return [.. this];
        }

        VoterStorageEntryF? firstEntry = GetFirstVoter();

        if (firstEntry == null)
            return [];

        var orderRemaining = this
            .Where(v => !OriginComparer.Instance.Equals(v.Key, firstEntry.Value.Key))
            .OrderByDescending(v => v.Value.Marker.MarkerValue)
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
    public OrderedVoterStorageF GetOrderedRankedVoterList()
    {
        var ranksOnly = this
            .Where(v => v.Value.Marker.MarkerType == MarkerType.Rank)
            .OrderBy(v => v.Value.Marker.MarkerValue)
            .ThenBy(v => v.Key, OriginComparer.Instance);
        var others = this
            .Where(v => v.Value.Marker.MarkerType != MarkerType.Rank)
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
    public CollectedVoterStorageF GetNonRankUsers()
    {
        return this.Where(s => s.Key.IsUser &&
                               nonRankMarkerTypes.Contains(s.Value.Marker.MarkerType));
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
    private VoterStorageEntryF? GetFirstVoter()
    {
        if (Count == 0)
            return null;

        var entries = this.Where(v => v.Key.IsPlan);

        if (!entries.Any())
        {
            entries = this;
        }

        var sorted = entries.OrderBy(v => v.Key.PostId, PostIdComparer.Instance);

        return sorted.FirstOrDefault();
    }
    #endregion Support functions
}

/// <summary>
/// Static class for an extension methods on voter storage data.
/// </summary>
public static class VoterStorageCreate
{
    /// <summary>
    /// Extension method that can take an enumerable of voter storage
    /// key/value pairs and turn it into a <see cref="VoterStorage"/> object.
    /// </summary>
    /// <param name="collectedVoterStorage">An enumeration of key/value pairs of voter storage info.</param>
    /// <returns>A <see cref="VoterStorage"/> object.</returns>
    public static VoterStorage AsVoterStorage(this CollectedVoterStorageF collectedVoterStorage)
    {
        return new VoterStorage(collectedVoterStorage);
    }

    /// <summary>
    /// Create a deep copy of the provided <see cref="VoterStorage"/> object.
    /// </summary>
    /// <returns>A deep copy of the provided <see cref="VoterStorage"/> object.</returns>
    public static VoterStorage Copy(this CollectedVoterStorageF copyFrom)
    {
        var copy = new VoterStorage();

        foreach (var (origin, vote) in copyFrom)
        {
            copy.Add(origin, vote);
        }

        return copy;
    }
}

