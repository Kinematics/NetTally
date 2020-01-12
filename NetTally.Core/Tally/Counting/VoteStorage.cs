using System.Collections.Generic;
using System.Linq;
using NetTally.Forums;

namespace NetTally.Votes
{
    public class VoteStorage : Dictionary<VoteLineBlock, VoterStorage>
    {
        const double categoryThreshold = 0.83;
        bool dirty = false;

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public VoteStorage()
        { }

        /// <summary>
        /// Constructor that allows copying from an existing instance.
        /// </summary>
        /// <param name="copyFrom">The VoteStorage instance to copy from.</param>
        public VoteStorage(VoteStorage copyFrom)
            : base(copyFrom)
        { }

        /// <summary>
        /// Constructor that allows copying from the base dictionary class
        /// that VoteStorage is based on.
        /// </summary>
        /// <param name="copyFrom">The Dictionary instance to copy from.</param>
        public VoteStorage(Dictionary<VoteLineBlock, VoterStorage> copyFrom)
            : base(copyFrom)
        { }
        #endregion

        #region Add/Remove votes
        /// <summary>
        /// Add the supplied voter to the specified vote.
        /// </summary>
        /// <param name="vote">The vote being updated.</param>
        /// <param name="supporter">The voter being added.</param>
        internal void AddSupporterToVote(VoteLineBlock vote, Origin supporter)
        {
            // If the vote isn't already in storage, create a new instance.
            if (!TryGetValue(vote, out var localVoters))
            {
                var referenceVote = vote.WithMarker("", MarkerType.None, 0);

                localVoters = new VoterStorage();

                Add(referenceVote, localVoters);
            }

            localVoters[supporter] = vote;
            dirty = true;
        }

        /// <summary>
        /// Remove the specified supporter from the given vote.
        /// </summary>
        /// <param name="vote">The vote being updated.</param>
        /// <param name="supporter">The voter being removed.</param>
        /// <returns>Returns true if the voter was removed from the vote,
        /// or false if the voter was not found.</returns>
        internal bool RemoveSupporterFromVote(VoteLineBlock vote, Origin supporter)
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
        /// <param name="voter">The voter being removed.</param>
        /// <returns>Returns true if the voter was removed from any votes.</returns>
        public bool RemoveVoterFromVotes(Origin voter)
        {
            bool removedAny = false;

            foreach (var vote in this)
            {
                if (vote.Value.Remove(voter))
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
        /// <returns>Returns true if any unsupported votes were found and removed.</returns>
        public bool RemoveUnsupportedVotes()
        {
            bool removedAny = false;

            // Any votes that no longer have any support can be removed
            var unsupported = this.Where(v => v.Value.Count == 0).ToList();

            foreach (var vote in unsupported)
            {
                if (Remove(vote.Key))
                {
                    dirty = true;
                    removedAny = true;
                }
            }

            return removedAny;
        }
        #endregion Add/Remove votes

        #region Queries
        /// <summary>
        /// Request a list of all votes currently in storage.
        /// All votes will be updated with the current Category value before being returned.
        /// </summary>
        /// <returns>Returns an IEnumerable of <seealso cref="VoteLineBlock"/> votes stored.</returns>
        public IEnumerable<VoteLineBlock> GetAllVotes()
        {
            foreach (var (vote, supporters) in this)
            {
                if (dirty)
                {
                    vote.Category = GetCategoryOf(supporters);
                }

                yield return vote;
            }

            dirty = false;

            // Private function to calculate the category for each set of supporters.
            static MarkerType GetCategoryOf(VoterStorage supporters)
            {
                var supportingUsers = supporters.Where(s => s.Key.AuthorType == IdentityType.User);
                int total = supportingUsers.Count();

                if (total == 0)
                    return MarkerType.None;

                var supporterMarkers = supportingUsers.GroupBy(s => s.Value.MarkerType);

                foreach (var supporterMarker in supporterMarkers)
                {
                    if (((double)supporterMarker.Count() / total) >= categoryThreshold)
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
        /// <returns>Returns an IEnumerable of <seealso cref="Origin"/>s stored.</returns>
        public IEnumerable<Origin> GetAllVoters()
        {
            return this.SelectMany(a => a.Value.Keys).Distinct();
        }

        /// <summary>
        /// Request a list of all voter origins currently supporting a given vote.
        /// Does not filter for plans.
        /// </summary>
        /// <param name="vote">The vote being checked on.</param>
        /// <returns>Returns an IEnumerable of the Origins of the supporters of the vote, if any.</returns>
        public IEnumerable<Origin> GetVotersFor(VoteLineBlock vote)
        {
            if (TryGetValue(vote, out var supporters))
            {
                return supporters.Select(a => a.Key);
            }

            return Enumerable.Empty<Origin>();
        }

        /// <summary>
        /// Request the <seealso cref="VoterStorage"/> collection of voters and votes
        /// for a given vote.
        /// </summary>
        /// <param name="vote">The vote being checked on.</param>
        /// <returns>Returns the supporters for the vote, if found. Otherwise null.</returns>
        public VoterStorage? GetSupportersFor(VoteLineBlock vote)
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
        /// <returns>Returns the number of users supporting a vote, or 0 if the vote is not found.</returns>
        public int GetSupportCountFor(VoteLineBlock vote)
        {
            if (TryGetValue(vote, out var supporters))
            {
                return supporters.Count(s => s.Key.AuthorType == IdentityType.User);
            }

            return 0;
        }

        /// <summary>
        /// Get the list of votes that a given voter supports.
        /// </summary>
        /// <param name="voter">The voter being checked on.</param>
        /// <returns>Returns a List of all the votes that the voter supports.</returns>
        public List<VoteLineBlock> GetVotesBy(Origin voter)
        {
            var result = this.SelectMany(a => a.Value)
                             .Where(a => a.Key == voter)
                             .Select(a => a.Value)
                             .ToList();
            return result;
        }

        /// <summary>
        /// Determines if a given voter supports a specified vote.
        /// </summary>
        /// <param name="voter">The voter being checked.</param>
        /// <param name="vote">The vote being checked.</param>
        /// <returns>Returns true if the voter supports the specified vote.</returns>
        public bool DoesVoterSupportVote(Origin voter, VoteLineBlock vote)
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
        public VoteLineBlock? GetVoteMatching(VoteLineBlock searchVote)
        {
            return Keys.FirstOrDefault(k => k == searchVote);
        }
        #endregion Queries
    }
}
