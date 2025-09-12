using NetTally.Tally.Components.Posts;
using NetTally.Tally.Vote.Components;

namespace NetTally.Tally.Components.Storage;

/// <summary>
/// Used in conjunction with <seealso cref="VoteStorage"/>, for
/// keeping track of voters and their associated votes.
/// VoterStorage is a dictionary of voter origins and the vote
/// each submitted.
/// </summary>
public class VoterStorage : Dictionary<Origin, VoteBlockType>
{
    #region Constructors
    /// <summary>
    /// Default constructor
    /// </summary>
    public VoterStorage()
        : base(OriginNameComparer.Instance)
    { }

    /// <summary>
    /// Constructor that takes a provided collection of voter storage elements.
    /// </summary>
    /// <param name="collectedVoterStorage">Voter storage elements to initialize
    /// the <see cref="VoterStorage"/> object with.</param>
    public VoterStorage(VoterStorageType collectedVoterStorage)
        : base(collectedVoterStorage, OriginNameComparer.Instance)
    { }
    #endregion Constructors

    #region Queries - Has XX?
    /// <summary>
    /// Check whether a given origin matches any voters stored in this lookup.
    /// </summary>
    /// <param name="origin">The origin to compare to.</param>
    /// <returns>Returns true if the origin exists in this lookup.</returns>
    public bool HasIdentity(Origin origin)
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
        var planAuthor = Authors.Create(planName);
        return HasPlan(planAuthor);
    }

    public bool HasPlan(Author planAuthor)
    {
        var origin = Origins.CreatePlanNameOnly(planAuthor);
        return ContainsKey(origin);
    }

    /// <summary>
    /// Check whether a given voter name exists in the voters stored in this lookup.
    /// </summary>
    /// <param name="voterName">The name of the voter to check for.</param>
    /// <returns>Returns true if the voter name can be found in this lookup.</returns>
    public bool HasVoter(string voterName)
    {
        var author = Authors.Create(voterName);
        return HasVoter(author);
    }

    public bool HasVoter(Author voterName)
    {
        var origin = Origins.CreateUserNameOnly(voterName);
        return ContainsKey(origin);
    }
    #endregion Queries - Has XX?

    #region Queries - Counts
    /// <summary>
    /// Get the total number of users who are vote supporters.
    /// </summary>
    /// <returns>The number of users in storage.</returns>
    public int GetUserCount()
    {
        return VoterAnalysis.GetUserCount(this);
    }

    /// <summary>
    /// Get the total number of users supporting the vote who
    /// used standard, score, or approval votes.
    /// </summary>
    /// <returns>The number of users making non-rank votes.</returns>
    public int GetNonRankUserCount()
    {
        return VoterAnalysis.GetNonRankUserCount(this);
    }

    /// <summary>
    /// Get the total number of positively supporting users in this vote.
    /// Approval +'s and Scores above 50 count for support.
    /// </summary>
    /// <returns>The number of users who expressed positive support.</returns>
    public int GetSupportCount()
    {
        return VoterAnalysis.GetSupportCount(this);
    }

    /// <summary>
    /// Gets the overall score for this vote.
    /// </summary>
    /// <returns>Returns a triplet of the score (int rounded version of the average),
    /// the average, and the lower 95% statistical margin.</returns>
    public (int score, double average, double lowerMargin) GetScore()
    {
        return VoterAnalysis.GetScore(this);
    }

    /// <summary>
    /// Get the overall approval for this vote.
    /// </summary>
    /// <returns>Returns the positive and negative results of how
    /// users voted for this vote.  A value above 50 is positive,
    /// while 50 and lower is negative.</returns>
    public (int positive, int negative) GetApproval()
    {
        return VoterAnalysis.GetApproval(this);
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
        return VoterAnalysis.GetOrderedVoterList(this);
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
        return VoterAnalysis.GetOrderedRankedVoterList(this);
    }
    #endregion Queries - Ordered Results

    #region Queries - General
    /// <summary>
    /// Get users from storage that used non-rank voting.
    /// </summary>
    /// <returns></returns>
    public VoterStorageType GetNonRankUsers()
    {
        return VoterAnalysis.GetNonRankUsers(this);
    }
    #endregion
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
    public static VoterStorage AsVoterStorage(this VoterStorageType collectedVoterStorage)
    {
        return new VoterStorage(collectedVoterStorage);
    }

    /// <summary>
    /// Create a deep copy of the provided <see cref="VoterStorage"/> object.
    /// </summary>
    /// <returns>A deep copy of the provided <see cref="VoterStorage"/> object.</returns>
    public static VoterStorage Copy(this VoterStorageType copyFrom)
    {
        var copy = new VoterStorage();

        foreach (var (origin, vote) in copyFrom)
        {
            copy.Add(origin, vote);
        }

        return copy;
    }
}

