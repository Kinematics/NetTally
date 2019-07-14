using System.Collections.Generic;
using System.Linq;
using NetTally.Forums;

namespace NetTally.Votes
{
    public class VoteStorage : Dictionary<VoteLineBlock, VoterStorage>
    {
        const double categoryThreshold = 0.83;

        public VoteStorage() { }

        public VoteStorage(VoteStorage copyFrom)
            : base(copyFrom)
        {
        }
        public VoteStorage(Dictionary<VoteLineBlock, VoterStorage> copyFrom)
            : base(copyFrom)
        {
        }

        public IEnumerable<VoteLineBlock> GetAllVotes()
        {
            foreach (var (vote, supporters) in this)
            {
                vote.Category = GetCategoryOf(supporters);
                yield return vote;
            }

            // Private function to calculate the category for each set of supporters.
            static MarkerType GetCategoryOf(VoterStorage supporters)
            {
                int total = supporters.Count;

                if (total == 0)
                    return MarkerType.None;

                var supporterMarkers = supporters.GroupBy(s => s.Value.MarkerType);

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

        public IEnumerable<Origin> GetAllVoters()
        {
            return this.SelectMany(a => a.Value.Keys).Distinct();
        }

        public VoterStorage? GetSupportersFor(VoteLineBlock vote)
        {
            if (TryGetValue(vote, out var supporters))
            {
                return supporters;
            }

            return null;
        }

        public IEnumerable<Origin> GetVotersFor(VoteLineBlock vote)
        {
            if (TryGetValue(vote, out var supporters))
            {
                return supporters.Select(a => a.Key);
            }

            return Enumerable.Empty<Origin>();
        }

        public List<VoteLineBlock> GetVotesBy(Origin voter)
        {
            var result = this.SelectMany(a => a.Value)
                             .Where(a => a.Key == voter)
                             .Select(a => a.Value)
                             .ToList();
            return result;
        }

        public bool RemoveVoterFromVotes(Origin voter)
        {
            bool removedAny = false;

            foreach (var vote in this)
            {
                if (vote.Value.Remove(voter))
                    removedAny = true;
            }

            return removedAny;
        }

        public bool RemoveUnsupportedVotes()
        {
            bool removedAny = false;

            // Any votes that no longer have any support can be removed
            var unsupported = this.Where(v => v.Value.Count == 0).ToList();

            foreach (var vote in unsupported)
            {
                if (Remove(vote.Key))
                    removedAny = true;
            }

            return removedAny;
        }


        public bool DoesVoterSupportVote(Origin voter, VoteLineBlock vote)
        {
            if (TryGetValue(vote, out var localVoters))
            {
                return localVoters.TryGetValue(voter, out _);
            }

            return false;
        }

        internal void AddSupporterToVote(VoteLineBlock vote, Origin supporter)
        {
            if (!TryGetValue(vote, out var localVoters))
            {
                // TODO: Do you need allLines anymore?
                var referenceVote = vote.WithMarker("", MarkerType.None, 0, allLines: true);

                localVoters = new VoterStorage();

                Add(referenceVote, localVoters);
            }

            localVoters[supporter] = vote;
        }

        internal bool RemoveSupporterFromVote(VoteLineBlock vote, Origin supporter)
        {
            if (TryGetValue(vote, out var localVoters))
            {
                return localVoters.Remove(supporter);
            }

            return false;
        }
    }



    public class VoterStorage : Dictionary<Origin, VoteLineBlock>
    {
        public VoterStorage() { }

        public VoterStorage(VoterStorage copyFrom)
            : base(copyFrom)
        {
        }

        public VoterStorage(Dictionary<Origin, VoteLineBlock> copyFrom)
            : base(copyFrom)
        {
        }

        readonly HashSet<Origin> NameLookup = new HashSet<Origin>();

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

        public new bool Remove(Origin key)
        {
            NameLookup.Remove(key);
            return base.Remove(key);
        }

        public new bool Remove(Origin key, out VoteLineBlock value)
        {
            NameLookup.Remove(key);
            return base.Remove(key, out value);
        }

        public bool HasIdentity(Origin origin)
        {
            return NameLookup.Contains(origin);
        }

        public bool HasPlan(string planName)
        {
            return NameLookup.Contains(new Origin(planName, IdentityType.Plan));
        }

        public bool HasVoter(string voterName)
        {
            return NameLookup.Contains(new Origin(voterName, IdentityType.User));
        }
    }

}
