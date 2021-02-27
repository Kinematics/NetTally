using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.Votes
{
    // Simplified generic references
    using VoterStorageEntry = KeyValuePair<Origin, VoteLineBlock>;
    using OrderedVoterStorage = List<KeyValuePair<Origin, VoteLineBlock>>;
    using FilteredVoterStorage = IEnumerable<KeyValuePair<Origin, VoteLineBlock>>;


    /// <summary>
    /// Used in conjunction with <seealso cref="VoteStorage"/>, for
    /// keeping track of voters and their associated votes.
    /// VoterStorage is a dictionary of voter origins and the vote
    /// each submitted.
    /// </summary>
    public class VoterStorage : Dictionary<Origin, VoteLineBlock>
    {
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public VoterStorage()
        { }

        /// <summary>
        /// Constructor that allows copying from an existing instance.
        /// </summary>
        /// <param name="copyFrom">The VoterStorage instance to copy from.</param>
        public VoterStorage(VoterStorage copyFrom)
            : base(copyFrom)
        { }

        /// <summary>
        /// Constructor that allows copying from the base dictionary class
        /// that VoterStorage is based on.
        /// </summary>
        /// <param name="copyFrom">The Dictionary instance to copy from.</param>
        public VoterStorage(Dictionary<Origin, VoteLineBlock> copyFrom)
            : base(copyFrom)
        { }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Special lookup for the origin keys stored in the VoterStorage dictionary.
        /// </summary>
        readonly HashSet<Origin> NameLookup = new HashSet<Origin>();
        #endregion Properties

        #region Reset
        public void Reset()
        {
            this.Clear();
            NameLookup.Clear();
        }
        #endregion

        #region Override Add/Remove functions
        /// <summary>
        /// Define the indexer to allow client code to use [] notation. 
        /// </summary>
        /// <param name="index">The index into the storage array.</param>
        /// <returns>Returns the element found at the given index.</returns>
        public new VoteLineBlock this[Origin index]
        {
            get
            {
                return base[index];
            }
            set
            {
                NameLookup.Add(index);
                base[index] = value;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the storage.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <param name="value">Stored value.</param>
        public new void Add(Origin key, VoteLineBlock value)
        {
            NameLookup.Add(key);
            base.Add(key, value);
        }

        /// <summary>
        /// Tries to add the specified key and value to the storage.
        /// </summary>
        /// <param name="key">Lookup key.</param>
        /// <param name="value">Stored value.</param>
        /// <returns>Returns true if successfully added. Returns false if the key already exists.</returns>
        public bool TryAdd(Origin key, VoteLineBlock value)
        {
            if (NameLookup.Add(key))
            {
                if (ContainsKey(key))
                    return false;

                base.Add(key, value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Remove the specified key from storage.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <returns>Returns true if the key was found in the collection.</returns>
        public new bool Remove(Origin key)
        {
            NameLookup.Remove(key);
            return base.Remove(key);
        }

        /// <summary>
        /// Removes the specified key from storage.
        /// Returns the corresponding value as an out parameter.
        /// </summary>
        /// <param name="key">The lookup key.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns>Returns true if the key was found in the collection.</returns>
        public bool Remove(Origin key, out VoteLineBlock value)
        {
            if (ContainsKey(key))
            {
                NameLookup.Remove(key);

                value = this[key];

                return base.Remove(key);
            }

            value = null;

            return false;
        }
        #endregion Override Add/Remove functions

        #region Queries - Has XX?
        /// <summary>
        /// Check whether a given origin matches any voters stored in this lookup.
        /// </summary>
        /// <param name="origin">The origin to compare to.</param>
        /// <returns>Returns true if the origin exists in this lookup.</returns>
        public bool HasIdentity(Origin origin)
        {
            return NameLookup.Contains(origin);
        }

        /// <summary>
        /// Check whether a given plan name exists in the voters stored in this lookup.
        /// </summary>
        /// <param name="planName">The name of the plan to check for.</param>
        /// <returns>Returns true if the plan name can be found in this lookup.</returns>
        public bool HasPlan(string planName)
        {
            return NameLookup.Contains(new Origin(planName, IdentityType.Plan));
        }

        /// <summary>
        /// Check whether a given voter name exists in the voters stored in this lookup.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns true if the voter name can be found in this lookup.</returns>
        public bool HasVoter(string voterName)
        {
            return NameLookup.Contains(new Origin(voterName, IdentityType.User));
        }
        #endregion Queries - Has XX?

        #region Queries - Counts
        /// <summary>
        /// Get the total number of vote supporters.
        /// </summary>
        /// <returns></returns>
        public int GetTotalCount()
        {
            return Count;
        }

        /// <summary>
        /// Get the total number of users who are vote supporters.
        /// </summary>
        /// <returns></returns>
        public int GetUserCount()
        {
            return this.Count(s => s.Key.AuthorType == IdentityType.User);
        }

        /// <summary>
        /// Get the total number of users supporting the vote who
        /// used standard, score, or approval votes.
        /// </summary>
        /// <returns></returns>
        public int GetNonRankUserCount()
        {
            return GetNonRankUsers().Count();
        }

        /// <summary>
        /// Get the total number of positively supporting users in this vote.
        /// Approval +'s and Scores above 50 count for support.
        /// </summary>
        /// <returns></returns>
        public int GetSupportCount()
        {
            return this.Count(s =>
                s.Key.AuthorType == IdentityType.User &&
                (s.Value.MarkerType == MarkerType.Vote ||
                 s.Value.MarkerType == MarkerType.Score ||
                 s.Value.MarkerType == MarkerType.Approval) &&
                s.Value.MarkerValue > 50);
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

            var (rating, lowerBound) = VoteCounting.RankVotes.Reference.RankingCalculations.GetLowerWilsonScore(users, a => a.Value.MarkerValue);

            foreach (var (userOrigin, userVote) in users)
            {
                count++;
                accum += userVote.MarkerValue;
            }

            if (count == 0)
                return (0, 0, 0);

            double average = (double)accum / count;
            int simpleScore = (int) Math.Round(average, 0, MidpointRounding.AwayFromZero);

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
                if (userVote.MarkerValue > 50)
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
        public OrderedVoterStorage GetOrderedVoterList()
        {
            var voterList = new OrderedVoterStorage();

            if (Count == 0)
            {
                return voterList;
            }

            if (Count == 1)
            {
                voterList.AddRange(this);
                return voterList;
            }

            var (firstVoter, firstVote) = GetFirstVoter();

            var orderRemaining = this.Where(v => v.Key != firstVoter)
                .OrderByDescending(v => v.Value.MarkerValue)
                .ThenBy(v => v.Key);

            voterList.Add(new VoterStorageEntry(firstVoter, firstVote));
            voterList.AddRange(orderRemaining);

            return voterList;
        }

        /// <summary>
        /// Gets an ordered version of the provided voters.
        /// The list is ordered by the rank value each voter used, then alphabetically.
        /// Any non-rank votes are added at the end.
        /// </summary>
        /// <param name="voters">The voters being ordered.</param>
        /// <returns>Returns an ordered list of the voters.</returns>
        public OrderedVoterStorage GetOrderedRankedVoterList()
        {
            var result = new OrderedVoterStorage();

            var ranksOnly = this.Where(v => v.Value.MarkerType == MarkerType.Rank).OrderBy(v => v.Value.MarkerValue).ThenBy(v => v.Key);
            var others = this.Where(v => v.Value.MarkerType != MarkerType.Rank).OrderBy(v => v.Key);

            result.AddRange(ranksOnly);
            result.AddRange(others);

            return result;
        }
        #endregion Queries - Ordered Results

        #region Queries - General
        /// <summary>
        /// Get users from storage that used non-rank voting.
        /// </summary>
        /// <returns></returns>
        public FilteredVoterStorage GetNonRankUsers()
        {
            return this.Where(s => (s.Key.AuthorType == IdentityType.User) &&
                                   ((s.Value.MarkerType == MarkerType.Vote) ||
                                    (s.Value.MarkerType == MarkerType.Score) ||
                                    (s.Value.MarkerType == MarkerType.Approval))
                             );
        }
        #endregion

        #region Support functions
        /// <summary>
        /// Get the first voter from the provided list of VoterStorage entries.
        /// Plans always have priority over users.
        /// </summary>
        /// <param name="voters">The VoterStorage collection of voters.</param>
        /// <returns>Returns the earliest VoterStorageEntry found.</returns>
        private (Origin voter, VoteLineBlock vote) GetFirstVoter()
        {
            if (!this.Any())
                throw new InvalidOperationException("No voters to process");

            Origin firstVoter = this.First().Key;

            foreach (var (voterOrigin, voterVote) in this)
            {
                // Plans have priority in determining first voter.
                if (voterOrigin.AuthorType == IdentityType.Plan)
                {
                    if (firstVoter.AuthorType != IdentityType.Plan)
                    {
                        firstVoter = voterOrigin;
                    }
                    else if (voterOrigin.ID < firstVoter.ID)
                    {
                        firstVoter = voterOrigin;
                    }
                }
                // If the firstVoter is already a plan, don't overwrite with a user.
                // Otherwise update if the new vote is earlier than the existing one.
                else if (firstVoter.AuthorType != IdentityType.Plan && voterOrigin.ID < firstVoter.ID)
                {
                    firstVoter = voterOrigin;
                }
            }

            return (firstVoter, this[firstVoter]);
        }
        #endregion Support functions
    }

    /// <summary>
    /// Extension methods that allow use of the VoterStorage methods on arbitrary
    /// IEnumerables of VoterStorageEntries.
    /// </summary>
    public static class VoterStorageExtensions
    {
        #region Queries - Counts
        /// <summary>
        /// Get the total number of vote supporters.
        /// </summary>
        /// <returns></returns>
        public static int GetTotalCount(this IEnumerable<VoterStorageEntry> storageValues)
        {
            if (storageValues is List<VoterStorageEntry> storageValuesList)
                return storageValuesList.Count;

            return storageValues.Count();
        }

        /// <summary>
        /// Get the total number of users who are vote supporters.
        /// </summary>
        /// <returns></returns>
        public static int GetUserCount(this IEnumerable<VoterStorageEntry> storageValues)
        {
            return storageValues.Count(s => s.Key.AuthorType == IdentityType.User);
        }

        /// <summary>
        /// Get the total number of users supporting the vote who
        /// used standard, score, or approval votes.
        /// </summary>
        /// <returns></returns>
        public static int GetNonRankUserCount(this IEnumerable<VoterStorageEntry> storageValues)
        {
            return storageValues.GetNonRankUsers().Count();
        }

        /// <summary>
        /// Get the total number of positively supporting users in this vote.
        /// Approval +'s and Scores above 50 count for support.
        /// </summary>
        /// <returns></returns>
        public static int GetSupportCount(this IEnumerable<VoterStorageEntry> storageValues)
        {
            return storageValues.Count(s =>
                (s.Value.MarkerType == MarkerType.Vote || s.Value.MarkerType == MarkerType.Score || s.Value.MarkerType == MarkerType.Approval)
                && s.Value.MarkerValue > 50);
        }

        /// <summary>
        /// Gets the overall score for this vote.
        /// </summary>
        /// <returns>Returns a triplet of the score (int rounded version of the average),
        /// the average, and the lower 95% statistical margin.</returns>
        public static (int score, double average, double lowerMargin) GetScore(
            this IEnumerable<VoterStorageEntry> storageValues)
        {
            var users = storageValues.GetNonRankUsers();

            int count = 0;
            int accum = 0;

            var (rating, lowerBound) = VoteCounting.RankVotes.Reference.RankingCalculations.GetLowerWilsonScore(users, a => a.Value.MarkerValue);

            foreach (var (userOrigin, userVote) in users)
            {
                count++;
                accum += userVote.MarkerValue;
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
        public static (int positive, int negative) GetApproval(this IEnumerable<VoterStorageEntry> storageValues)
        {
            var users = storageValues.GetNonRankUsers();

            int positive = 0;
            int negative = 0;

            // Standard votes have a value of 100, Approval+ have a value of 80, and Scores are variable.
            // Sum up the positive and negative results.
            foreach (var (userOrigin, userVote) in users)
            {
                if (userVote.MarkerValue > 50)
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
        public static OrderedVoterStorage GetOrderedVoterListEx(this IEnumerable<VoterStorageEntry> storageValues)
        {
            var voterList = new OrderedVoterStorage();

            if (!storageValues.Any())
            {
                return voterList;
            }

            var (firstVoter, firstVote) = storageValues.GetFirstVoter();

            var orderRemaining = storageValues.Where(v => v.Key != firstVoter).OrderBy(v => v.Key);

            voterList.Add(new VoterStorageEntry(firstVoter, firstVote));
            voterList.AddRange(orderRemaining);

            return voterList;
        }

        /// <summary>
        /// Gets an ordered version of the provided voters.
        /// The list is ordered by the rank value each voter used, then alphabetically.
        /// Any non-rank votes are added at the end.
        /// </summary>
        /// <param name="voters">The voters being ordered.</param>
        /// <returns>Returns an ordered list of the voters.</returns>
        public static OrderedVoterStorage GetOrderedRankedVoterList(
            this IEnumerable<VoterStorageEntry> storageValues)
        {
            var result = new OrderedVoterStorage();

            var ranksOnly = storageValues
                .Where(v => v.Value.MarkerType == MarkerType.Rank)
                .OrderBy(v => v.Value.MarkerValue)
                .ThenBy(v => v.Key);
            var others = storageValues
                .Where(v => v.Value.MarkerType != MarkerType.Rank)
                .OrderBy(v => v.Key);

            result.AddRange(ranksOnly);
            result.AddRange(others);

            return result;
        }
        #endregion Queries - Ordered Results

        #region Queries - General
        /// <summary>
        /// Get users from storage that used non-rank voting.
        /// </summary>
        /// <returns></returns>
        public static FilteredVoterStorage GetNonRankUsers(this IEnumerable<VoterStorageEntry> storageValues)
        {
            return storageValues.Where(s => (s.Key.AuthorType == IdentityType.User) &&
                                            ((s.Value.MarkerType == MarkerType.Vote) ||
                                             (s.Value.MarkerType == MarkerType.Score) ||
                                             (s.Value.MarkerType == MarkerType.Approval))
                                      );
        }
        #endregion

        #region Support functions
        /// <summary>
        /// Get the first voter from the provided list of VoterStorage entries.
        /// Plans always have priority over users.
        /// </summary>
        /// <param name="voters">The VoterStorage collection of voters.</param>
        /// <returns>Returns the earliest VoterStorageEntry found.</returns>
        private static (Origin voter, VoteLineBlock vote) GetFirstVoter(
            this IEnumerable<VoterStorageEntry> storageValues)
        {
            if (!storageValues.Any())
                throw new InvalidOperationException("No voters to process");

            var (firstVoter, firstVote) = storageValues.First();

            foreach (var (voterOrigin, voterVote) in storageValues)
            {
                // Plans have priority in determining first voter.
                if (voterOrigin.AuthorType == IdentityType.Plan)
                {
                    if (firstVoter.AuthorType != IdentityType.Plan)
                    {
                        firstVoter = voterOrigin;
                        firstVote = voterVote;
                    }
                    else if (voterOrigin.ID < firstVoter.ID)
                    {
                        firstVoter = voterOrigin;
                        firstVote = voterVote;
                    }
                }
                // If the firstVoter is already a plan, don't overwrite with a user.
                // Otherwise update if the new vote is earlier than the existing one.
                else if (firstVoter.AuthorType != IdentityType.Plan && voterOrigin.ID < firstVoter.ID)
                {
                    firstVoter = voterOrigin;
                    firstVote = voterVote;
                }
            }

            return (firstVoter, firstVote);
        }
        #endregion Support functions

    }
}
