using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public class VoteCounter2 : IVoteCounter2
    {
        public VoteCounter2()
        {
            Votes[VoteType.Vote] = new HashSet<Vote>();
            Votes[VoteType.Rank] = new HashSet<Vote>();
            Votes[VoteType.Plan] = Votes[VoteType.Vote];
            Voters[VoteType.Vote] = new Dictionary<string, string>();
            Voters[VoteType.Rank] = new Dictionary<string, string>();
            Voters[VoteType.Plan] = Voters[VoteType.Vote];

        }

        #region Variables and properties

        public Dictionary<VoteType, HashSet<Vote>> Votes { get; } = new Dictionary<VoteType, HashSet<Vote>>();
        public Dictionary<VoteType, Dictionary<string, string>> Voters { get; } = new Dictionary<VoteType, Dictionary<string, string>>();

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<PostComponents> VotePosts { get; private set; } = new List<PostComponents>();
        public List<PostComponents> FloatingReferences { get; } = new List<PostComponents>();

        public HashSet<Vote> GetVotes(VoteType voteType)
        {
            return Votes[voteType];
        }

        public Dictionary<string, string> GetVoters(VoteType voteType)
        {
            return Voters[voteType];
        }
        #endregion


        public void CountVotes(List<PostComponents> posts, IQuest quest, ThreadInfo threadInfo)
        {
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (threadInfo == null)
                throw new ArgumentNullException(nameof(threadInfo));

            Reset();

            var votePosts = from post in posts
                            where post.IsVote &&
                                  post.PostNumber >= quest.FirstTallyPost &&
                                  (quest.ReadToEndOfThread || post.PostNumber <= quest.EndPost) &&
                                  post.Author != threadInfo.Author
                            select post;

            var voters = votePosts.Select(p => p.Author).Distinct();

            foreach (var post in votePosts)
            {
                Vote vote = ProcessPost(post);
            }
        }

        private Vote ProcessPost(PostComponents post)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            foreach (var e in Enumerations.EnumToList<VoteType>())
            {
                Votes[e].Clear();
                Voters[e].Clear();
            }

            PlanNames.Clear();
            FloatingReferences.Clear();
        }

        public Vote Add(string vote, VoteType voteType)
        {
            Vote v = null;

            if (!HasVote(vote, voteType))
            {
                v = new Vote(vote, voteType);
                GetVotes(voteType).Add(v);
            }

            return v;
        }

        public bool Add(Vote vote)
        {
            if (vote == null)
                return false;

            Vote v;
            if (HasVote(vote.Text, vote.Type, out v))
            {
                v.MergeFrom(vote);
            }
            else
            {
                GetVotes(vote.Type).Add(vote);
            }

            return true;
        }

        public bool Delete(string vote, VoteType voteType)
        {
            Vote v;
            if (HasVote(vote, voteType, out v))
            {
                return GetVotes(v.Type).Remove(v);
            }

            return false;
        }

        public bool Delete(Vote vote)
        {
            return GetVotes(vote.Type).Remove(vote);
        }

        public bool HasVote(string vote, VoteType voteType, bool condensed = false)
        {
            if (condensed && voteType == VoteType.Rank)
                vote = VoteString.CondenseVote(vote);

            string key = VoteString.MinimizeVote(vote);
            var votes = GetVotes(voteType);

            return votes.Any(v => v.Minimized == key);
        }

        public bool HasVote(string vote, VoteType voteType, out Vote foundVote, bool condensed = false)
        {
            if (condensed && voteType == VoteType.Rank)
                vote = VoteString.CondenseVote(vote);

            string key = VoteString.MinimizeVote(vote);
            var votes = GetVotes(voteType);

            foundVote = votes.FirstOrDefault(v => v.Minimized == key);
            return foundVote != null;
        }

        public bool Join(List<string> voters, string voterToJoin, VoteType voteType)
        {
            throw new NotImplementedException();
        }

        public bool Merge(string fromVote, string toVote, VoteType voteType)
        {
            throw new NotImplementedException();
        }

        public bool Rename(string fromVote, string toVote, VoteType voteType)
        {
            throw new NotImplementedException();
        }


        public void Support(string vote, string voter, VoteType voteType)
        {
            throw new NotImplementedException();
        }

        public bool Unjoin(string voter, VoteType voteType)
        {
            throw new NotImplementedException();
        }

        public void Unsupport(string vote, string voter, VoteType voteType)
        {
            throw new NotImplementedException();
        }
    }
}
