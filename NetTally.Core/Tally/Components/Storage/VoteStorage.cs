using NetTally.Enums;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Votes;

namespace NetTally.Tally.Components.Storage;

public class VoteStorage : Dictionary<VoteBlockType, VoterStorage>
{
    const double CategoryThreshold = 0.83;
    bool dirty = false;

    #region Constructors
    /// <summary>
    /// Default constructor
    /// </summary>
    public VoteStorage()
        : base(VoteBlockComparer.Instance)
    { }

    /// <summary>
    /// Create a deep copy of the current <see cref="VoteStorage"/> object.
    /// </summary>
    /// <returns>A deep copy of the current <see cref="VoteStorage"/> object.</returns>
    public VoteStorage Copy()
    {
        return CopyFrom(this);
    }

    /// <summary>
    /// Creates a deep copy of the provided <see cref="VoteStorage"/> object.
    /// </summary>
    /// <param name="copyFrom">The <see cref="VoteStorage"/> object to copy.</param>
    /// <returns>A deep copy of the provided <see cref="VoteStorage"/> object.</returns>
    public static VoteStorage CopyFrom(Dictionary<VoteBlockType, VoterStorage> copyFrom)
    {
        var copy = new VoteStorage();

        foreach (var (vote, storage) in copyFrom)
        {
            copy.Add(vote, storage.Copy());
        }

        return copy;
    }
    #endregion

    #region Add/Remove votes
    /// <summary>
    /// Add the supplied voter to the specified vote.
    /// </summary>
    /// <param name="vote">The vote being updated.</param>
    /// <param name="supporter">The voter being added.</param>
    internal void AddSupporterToVote(VoteBlockType vote, Origin supporter)
    {
        // If the vote isn't already in storage, create a new instance.
        if (!TryGetValue(vote, out var localVoters))
        {
            var referenceVote = vote with { Marker = Marker.Empty };

            localVoters = [];

            Add(referenceVote, localVoters);
        }

        localVoters[supporter] = vote;
        dirty = true;
    }

    /// <summary>
    /// Add the supplied voters to the specified vote.
    /// </summary>
    /// <param name="vote">The vote being updated.</param>
    /// <param name="supporter">The voter being added.</param>
    internal void AddSupportersToVote(VoteBlockType vote, IEnumerable<Origin> supporters)
    {
        // If the vote isn't already in storage, create a new instance.
        if (!TryGetValue(vote, out var localVoters))
        {
            var referenceVote = vote with { Marker = Marker.Empty };

            localVoters = [];

            Add(referenceVote, localVoters);
        }

        foreach (var supporter in supporters)
        {
            localVoters[supporter] = vote;
        }

        dirty = true;
    }

    /// <summary>
    /// Remove the specified supporter from the given vote.
    /// </summary>
    /// <param name="vote">The vote being updated.</param>
    /// <param name="supporter">The voter being removed.</param>
    /// <returns>Returns <c>true</c> if the voter was removed from the vote,
    /// or <c>false</c> if the voter was not found.</returns>
    internal bool RemoveSupporterFromVote(Origin supporter, VoteBlockType vote)
    {
        if (TryGetValue(vote, out var localVoters))
        {
            dirty = true;
            return localVoters.Remove(supporter);
        }

        return false;
    }

    /// <summary>
    /// Remove the specified voter from all votes.
    /// </summary>
    /// <param name="supporter">The voter being removed.</param>
    /// <returns>Returns <c>true</c> if the voter was removed from any votes.</returns>
    public bool RemoveSupporterFromAllVotes(Origin supporter)
    {
        bool removedAny = false;

        foreach (var (vote, storage) in this)
        {
            if (storage.Remove(supporter))
            {
                dirty = true;
                removedAny = true;
            }
        }

        return removedAny;
    }

    /// <summary>
    /// Remove all votes that do not currently have any supporters.
    /// </summary>
    /// <returns>Returns <c>true</c> if any unsupported votes were found and removed.</returns>
    public bool RemoveUnsupportedVotes()
    {
        bool removedAny = false;

        var unsupported = GetVotesWithSupportCount(0);

        foreach (var (vote, storage) in unsupported)
        {
            if (Remove(vote))
            {
                dirty = true;
                removedAny = true;
            }
        }

        return removedAny;
    }

    private List<VoteStorageEntryF> GetVotesWithSupportCount(int count)
    {
        return this.Where(v => v.Value.Count == count).ToList();
    }
    #endregion Add/Remove votes

    #region Queries
    /// <summary>
    /// Request a list of all votes currently in storage.
    /// All votes will be updated with the current Category value before being returned.
    /// </summary>
    /// <returns>Returns an IEnumerable of <seealso cref="VoteBlockType"/> votes stored.</returns>
    public IEnumerable<VoteBlockType> GetAllVotes()
    {
        if (dirty)
        {
            foreach (var (vote, supporters) in this)
            {
                vote.Category = GetCategoryOf(supporters);
            }

            dirty = false;
        }

        return [.. Keys];

        // Private function to calculate the category for each set of supporters.
        static MarkerType GetCategoryOf(VoterStorage supporters)
        {
            var supportingUsers = supporters.Where(s => s.Key.IsUser);
            int total = supportingUsers.Count();

            if (total == 0)
                return MarkerType.None;

            var supporterMarkers = supportingUsers.GroupBy(s => s.Value.Marker.MarkerType);

            foreach (var supporterMarker in supporterMarkers)
            {
                if (((double)supporterMarker.Count() / total) >= CategoryThreshold)
                {
                    return supporterMarker.Key;
                }
            }

            return MarkerType.Vote;
        }
    }

    /// <summary>
    /// Request a list of all voter origins currently being stored.
    /// Does not filter for plans.
    /// </summary>
    /// <returns>Returns a list of <seealso cref="Origin"/>s stored.</returns>
    public IEnumerable<Origin> GetAllVoters()
    {
        return this.SelectMany(a => a.Value.Keys)
            .Distinct(OriginComparer.Instance);
    }

    /// <summary>
    /// Request a list of all voter origins currently supporting a given vote.
    /// Does not filter for plans.
    /// </summary>
    /// <param name="vote">The vote being checked on.</param>
    /// <returns>Returns an IEnumerable of the <see cref="Origin"> of
    /// the supporters of the vote, if any.</returns>
    public IEnumerable<Origin> GetVotersFor(VoteBlockType vote)
    {
        if (TryGetValue(vote, out var supporters))
        {
            return supporters.Keys;
        }

        return [];
    }

    /// <summary>
    /// Request a list of all voter origins currently supporting a given vote.
    /// Does filter for plans.
    /// </summary>
    /// <param name="vote">The vote being checked on.</param>
    /// <returns>Returns an IEnumerable of the <see cref="Origin"> of
    /// the supporters of the vote, if any.</returns>
    public IEnumerable<Origin> GetUserVotersFor(VoteBlockType vote)
    {
        if (TryGetValue(vote, out var supporters))
        {
            return supporters.Keys.Where(v => v.IsUser);
        }

        return [];
    }

    /// <summary>
    /// Request the <seealso cref="VoterStorage"/> collection of voters and votes
    /// for a given vote.
    /// </summary>
    /// <param name="vote">The vote being checked on.</param>
    /// <returns>Returns the supporters for the vote, if found. Otherwise null.</returns>
    /// TODO: Rename GetStorageFor
    public VoterStorage? GetSupportersFor(VoteBlockType vote)
    {
        if (TryGetValue(vote, out var supporters))
        {
            return supporters;
        }

        return null;
    }

    /// <summary>
    /// Gets the number of users supporting a given vote.
    /// </summary>
    /// <param name="vote">The vote being checked on.</param>
    /// <returns>Returns the number of users supporting a vote,
    /// or 0 if the vote is not found.</returns>
    public int GetUserSupportCountFor(VoteBlockType vote)
    {
        if (TryGetValue(vote, out var supporters))
        {
            return supporters.Count(s => s.Key.IsUser);
        }

        return 0;
    }

    /// <summary>
    /// Get the list of votes that a given voter supports.
    /// </summary>
    /// <param name="voter">The voter being checked on.</param>
    /// <returns>Returns a list of all the votes that the voter supports.</returns>
    public IEnumerable<VoteBlockType> GetVotesBy(Origin voter)
    {
        var result = this.SelectMany(a => a.Value)
                         .Where(a => OriginComparer.Instance.Equals(voter, a.Key))
                         .Select(a => a.Value);

        return result;
    }

    /// <summary>
    /// Determines if a given voter supports a specified vote.
    /// </summary>
    /// <param name="voter">The voter being checked.</param>
    /// <param name="vote">The vote being checked.</param>
    /// <returns>Returns true if the voter supports the specified vote.</returns>
    public bool DoesVoterSupportVote(Origin voter, VoteBlockType vote)
    {
        if (TryGetValue(vote, out var localVoters))
        {
            return localVoters.HasIdentity(voter);
        }

        return false;
    }

    /// <summary>
    /// Gets the vote that exists as a key in VoteStorage that matches
    /// the vote provided.
    /// </summary>
    /// <param name="searchVote">The vote that we're trying to get the actual key for.</param>
    /// <returns>Returns the vote matching the vote provided, or null if not found.</returns>
    public VoteBlockType? GetVoteMatching(VoteBlockType searchVote)
    {
        return Keys.FirstOrDefault(k => VoteBlockComparer.Instance.Equals(k, searchVote));
    }
    #endregion Queries
}
