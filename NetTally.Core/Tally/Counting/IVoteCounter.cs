using System.Collections.Generic;
using System.ComponentModel;
using NetTally.Votes;
using NetTally.Experiment3;

namespace NetTally.VoteCounting
{
    public interface IVoteCounter : INotifyPropertyChanged
    {
        /// <summary>
        /// The quest the vote counter is set to track.
        /// </summary>
        IQuest? Quest { get; set; }
        /// <summary>
        /// The title of the quest thread when tallied.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Track whether the vote counter is currently being used for a tally operation.
        /// </summary>
        bool VoteCounterIsTallying { get; set; }
        /// <summary>
        /// Track whether a tally was cancelled.
        /// </summary>
        bool TallyWasCanceled { get; set; }

        /// <summary>
        /// A collection structure to store votes and the voters who voted for them.
        /// Also stores the specific variant that each voter used.
        /// </summary>
        public Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> VoteBlockSupporters { get; }
        /// <summary>
        /// Record any posts that make references to future posts, and thus can't be processed
        /// in the original post order.
        /// </summary>
        HashSet<Post> FutureReferences { get; }
        /// <summary>
        /// The list of tasks that have been recorded for the tally, whether drawn from
        /// votes as they are tallied, or manually entered by the user.
        /// </summary>
        IReadOnlyList<string> TaskList { get; }


        /// <summary>
        /// Reset internal storage for a new tally.
        /// </summary>
        void Reset();
        /// <summary>
        /// Reset user-defined tasks if the provided quest name is different than the current quest name.
        /// </summary>
        /// <param name="forQuestName">The name of the quest the tally is about to be run for.</param>
        void ResetUserDefinedTasks(string forQuestName);
        /// <summary>
        /// Clear any user merge information we've retained, so that it doesn't get used to auto-merge.
        /// </summary>
        void ResetUserMerges();

        /// <summary>
        /// The list of posts collected from the quest. Read-only.
        /// </summary>
        IReadOnlyList<Post> Posts { get; }
        /// <summary>
        /// Add a new set of posts for the <see cref="IVoteCounter"/> to use.
        /// </summary>
        /// <param name="posts">The posts to be stored in the <see cref="IVoteCounter"/>.</param>
        void AddPosts(IEnumerable<Post> posts);
        /// <summary>
        /// Request that the currently stored posts be cleared.
        /// </summary>
        void ClearPosts();

        /// <summary>
        /// Store a plan's information to allow it to be looked up by plan name or post ID.
        /// If the plan name has already been entered, will not update anything and return false.
        /// </summary>
        /// <param name="planName">The canonical name of the plan.</param>
        /// <param name="postID">The post ID the plan was defined in.</param>
        /// <param name="planBlock">The the vote line block that defines the plan.</param>
        /// <returns>Returns true if it was added, or false if it already exists.</returns>
        bool AddReferencePlan(string planName, int postID, VoteLineBlock planBlock);
        /// <summary>
        /// Store a voter and their post ID.
        /// This is expecting to be called for every vote by the user,
        /// so the post ID will eventually be that user's last vote in the tally.
        /// </summary>
        /// <param name="voterName">The proper name of the voter.</param>
        /// <param name="postID">The ID of their vote post.</param>
        /// <returns>Returns true if the voter was added, or false if the voter already exists.</returns>
        bool AddReferenceVoter(string voterName, int postID);
        /// <summary>
        /// Add a post to a store of future references made.
        /// </summary>
        /// <param name="post">The post to store.</param>
        /// <returns>Returns true if the post was added, or false if it already exists.</returns>
        bool AddFutureReference(Post post);

        /// <summary>
        /// Get canonical version of the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan being checked for.</param>
        /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
        string? GetProperPlanName(string planName);
        /// <summary>
        /// Get canonical version of the provided voter name.
        /// </summary>
        /// <param name="voterName">The name of the voter being checked for.</param>
        /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
        string? GetProperVoterName(string voterName);

        /// <summary>
        /// Get the post ID stored for the specified plan, which is the post that it was defined in.
        /// </summary>
        /// <param name="planName">The name of the plan to check on.</param>
        /// <returns>Returns the post ID for where the plan was defined, or null if not found.</returns>
        int GetPlanReferencePostId(string planName);
        /// <summary>
        /// Get the post ID stored for the specified voter.  This will always be the last one entered.
        /// </summary>
        /// <param name="voterName">The name of the voter to check on.</param>
        /// <returns>Returns the post ID for the voter, or null if not found.</returns>
        int GetVoterReferencePostId(string voterName);
        /// <summary>
        /// Get the ID of the post by the specified author at the time of the request.
        /// This may change over the course of a tally.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns the post ID if the voter's most recently processed post, or 0 if not found.</returns>
        int GetLatestVoterPostId(string voterName);
        /// <summary>
        /// Get the last post made by a given author.
        /// Possibly restrict the search range to no more than the specified post ID.
        /// </summary>
        /// <param name="voterName">The voter being queried.</param>
        /// <param name="maxPostId">The highest post ID allowed. 0 means unrestricted.</param>
        /// <returns>Returns the last post by the requested author, if found. Otherwise null.</returns>
        Post? GetLastPostByAuthor(string voterName, int maxPostId = 0);
        /// <summary>
        /// Get the reference plan corresponding to the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan to get.</param>
        /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
        VoteLineBlock? GetReferencePlan(string planName);
        /// <summary>
        /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
        /// </summary>
        /// <param name="voterName">The name of the voter or plan being requested.</param>
        /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
        List<VoteLineBlock> GetVotesBy(string voterName);

        IEnumerable<VoteLineBlock> GetSupportedVotesList();
        IEnumerable<string> GetFullVotersList();
        IEnumerable<string> GetVotersFor(VoteLineBlock vote);
        int GetTotalVoterCount();


        /// <summary>
        /// Determine if the requested plan name exists in the current list of plans.
        /// </summary>
        /// <param name="planName">The name of the plan to check for.</param>
        /// <returns>Returns true if the plan is known to exist.</returns>
        bool HasPlan(string? planName);
        /// <summary>
        /// Determines if the specified voter is present in the currently active votes.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns true if the voter has voted in the tally.</returns>
        bool HasVoter(string? voterName);
        /// <summary>
        /// Determines if there is a more recent vote made by the author of the provided post.
        /// </summary>
        /// <param name="post">The post made by some author.</param>
        /// <returns>Returns true if there is a more recent post made by the author of the post.</returns>
        bool HasNewerVote(Post post);


        /// <summary>
        /// Function to add the provided votes to the current vote stores.
        /// </summary>
        /// <param name="voteParts">The vote blocks to be added.</param>
        /// <param name="voter">The voter.</param>
        /// <param name="postID">The ID of the vote post.</param>
        /// <param name="voteType">The type of vote.</param>
        void AddVotes(IEnumerable<VoteLineBlock> voteParts, string voter, int postID);
        /// <summary>
        /// Merge the vote supporters from one vote into another.
        /// </summary>
        /// <param name="fromVote">The originating vote.</param>
        /// <param name="toVote">The destination vote.</param>
        /// <returns>Returns true if successfully completed.</returns>
        bool Merge(VoteLineBlock fromVote, VoteLineBlock toVote);
        /// <summary>
        /// Transfer the vote supporters from one vote into several other votes.
        /// </summary>
        /// <param name="fromVote">The originating vote.</param>
        /// <param name="toVotes">The destination votes.</param>
        /// <returns>Returns true if successfully completed.</returns>
        bool Split(VoteLineBlock fromVote, List<VoteLineBlock> toVotes);
        /// <summary>
        /// Shift support by various voters from their original vote to any votes
        /// supported by a specified target voter.
        /// </summary>
        /// <param name="voters">The voters that will support the new voter.</param>
        /// <param name="voterToJoin">The voter to join.</param>
        /// <returns>Returns true if successfully completed.</returns>
        bool Join(List<string> voters, string voterToJoin);
        /// <summary>
        /// Delete an entire vote and all associated supporters.
        /// </summary>
        /// <param name="vote">The vote to delete.</param>
        /// <returns>Returns true if successfully completed.</returns>
        bool Delete(VoteLineBlock vote);
        /// <summary>
        /// Undo a prior Merge, Join, or Delete action.
        /// </summary>
        /// <returns>Returns true if successfully completed.</returns>
        bool Undo();
        /// <summary>
        /// Check whether there are any stored undo actions.
        /// </summary>
        bool HasUndoActions { get; }

        bool ReplaceTask(VoteLineBlock vote, string task);

        bool AddUserDefinedTask(string task);
        void AddUserDefinedTasksToTaskList();
        void IncreaseTaskPosition(int currentPosition);
        void DecreaseTaskPosition(int currentPosition);
        void ResetTasksOrder(TasksOrdering order);

        List<string> TaskList { get; }


        HashSet<Post> FutureReferences { get; }




        #region deprecated
        Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType);
        Dictionary<string, string> GetVotersCollection(VoteType voteType);
        List<string> GetVotesFromReference(string voteLine, string author);
        bool HasRankedVotes { get; }

        #endregion
    }
}
