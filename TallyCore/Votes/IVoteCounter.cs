﻿using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;
using NetTally.Votes;

namespace NetTally
{
    public interface IVoteCounter
    {
        IQuest Quest { get; set; }

        Task<bool> TallyVotes(IQuest quest, ThreadRangeInfo startInfo, List<Task<HtmlDocument>> pages);
        void TallyPosts();
        void TallyPosts(IQuest quest);
        List<PostComponents> PostsList { get; }
        void Reset();

        void AddVotes(IEnumerable<string> voteParts, string voter, string postID, VoteType voteType);

        bool Merge(string fromVote, string toVote, VoteType voteType);
        bool Join(List<string> voters, string voterToJoin, VoteType voteType);
        bool Delete(string vote, VoteType voteType);

        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
        Dictionary<string, string> GetVotersCollection(VoteType voteType);

        List<string> GetCondensedRankVotes();
        List<string> GetVotesFromReference(string voteLine, string author);

        string Title { get; set; }
        HashSet<string> PlanNames { get; }

        bool HasRankedVotes { get; }
        bool HasPlan(string planName);
        bool HasVote(string vote, VoteType voteType);
        bool HasVoter(string voterName, VoteType voteType);
        bool HasNewerVote(PostComponents post);


        HashSet<string> ReferenceVoters { get; }
        Dictionary<string, string> ReferenceVoterPosts { get; }
        HashSet<string> ReferencePlanNames { get; }
        Dictionary<string, List<string>> ReferencePlans { get; }

        HashSet<PostComponents> FutureReferences { get; }

        Stack<UndoAction> UndoBuffer { get; }
        bool HasUndoActions { get; }
        bool Undo();
    }
}
