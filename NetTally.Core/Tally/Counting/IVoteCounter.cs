using System.ComponentModel;
using NetTally.Enums;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Processing;
using NetTally.Tally.Storage;
using NetTally.Tally.Vote.Component;
using NetTally.Utility.Collections;

namespace NetTally.Tally.Counting;

public interface IVoteCounter : INotifyPropertyChanged
{
    /// <summary>
    /// The quest the vote counter is set to track.
    /// </summary>
    Quest Quest { get; set; }

    /// <summary>
    /// The titles of the quest threads that have been tallied.
    /// </summary>
    List<string> Titles { get; }

    /// <summary>
    /// The list of tasks that have been recorded for the tally, whether drawn from
    /// votes as they are tallied, or manually entered by the user.
    /// </summary>
    ObservableCollectionExt<VoteTask> TaskList { get; }

    /// <summary>
    /// Access to the vote storage system.
    /// </summary>
    VoteStorage VoteStorage { get; }

    /// <summary>
    /// Reset internal storage for a new tally.
    /// </summary>
    void Reset();
    /// <summary>
    /// Reset user-defined tasks if the provided quest name is different than the current quest name.
    /// </summary>
    void ResetUserDefinedTasks();
    /// <summary>
    /// Clear any user merge information we've retained, so that it doesn't get used to auto-merge.
    /// </summary>
    void ResetUserMerges();

    /// <summary>
    /// Whether the quest has any recorded posts.
    /// </summary>
    bool HasPosts { get; }

    /// <summary>
    /// Access to the posts ready to be processed.
    /// </summary>
    List<PostToProcess> Posts { get; }

    /// <summary>
    /// Construct votes from existing posts.
    /// </summary>
    void ConstructVotes();

    /// <summary>
    /// Construct votes from the provided posts.
    /// </summary>
    /// <param name="titles">The titles to display during output.</param>
    /// <param name="posts">The posts to be processed.</param>
    void ConstructVotes(IEnumerable<string> titles, IEnumerable<Post> posts);

    /// <summary>
    /// Normalize the formatting of a plan name.
    /// </summary>
    /// <param name="originalPlanName">The original name of the plan.</param>
    /// <param name="originalVoteBlock">The original vote block.</param>
    /// <returns>A normalized version of the name and vote block contents.</returns>
    (string Name, VoteBlock Contents)?
        NormalizePlan(string originalPlanName, VoteBlock originalVoteBlock);

    /// <summary>
    /// Store a plan's information to allow it to be looked up by plan name or post ID.
    /// If the plan name has already been entered, will not update anything and return false.
    /// </summary>
    /// <param name="planName">The canonical name of the plan.</param>
    /// <param name="postID">The post ID the plan was defined in.</param>
    /// <param name="planBlock">The the vote line block that defines the plan.</param>
    /// <returns>Returns true if it was added, or false if it already exists.</returns>
    bool AddReferencePlan(Origin planOrigin, VoteBlock plan);
    /// <summary>
    /// Store a voter and their post ID.
    /// This is expecting to be called for every vote by the user,
    /// so the post ID will eventually be that user's last vote in the tally.
    /// </summary>
    /// <param name="voterName">The proper name of the voter.</param>
    /// <param name="postID">The ID of their vote post.</param>
    /// <returns>Returns true if the voter was added, or false if the voter already exists.</returns>
    bool AddReferenceVoter(Origin voter);

    /// <summary>
    /// Get canonical version of the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    Origin? GetPlanOriginByName(string? planName);
    /// <summary>
    /// Get canonical version of the provided voter name.
    /// </summary>
    /// <param name="voterName">The name of the voter being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    Origin? GetVoterOriginByName(string? voterName);
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
    /// Get the last post made by a given author.
    /// Possibly restrict the search range to no more than the specified post ID.
    /// </summary>
    /// <param name="voterName">The voter being queried.</param>
    /// <param name="maxPostId">The highest post ID allowed. 0 means unrestricted.</param>
    /// <returns>Returns the last post by the requested author, if found. Otherwise null.</returns>
    PostToProcess? GetLastPostByAuthor(Origin author, PostId maxPostId);
    /// <summary>
    /// Get the reference plan corresponding to the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan to get.</param>
    /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
    VoteBlock? GetReferencePlan(Origin planOrigin);
    IEnumerable<VoteBlock> GetReferencePlans();
    /// <summary>
    /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
    /// </summary>
    /// <param name="voterName">The name of the voter or plan being requested.</param>
    /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
    IEnumerable<VoteBlock> GetVotesBy(Origin voter);
    /// <summary>
    /// Get a collection of all the votes that currently have supporters.
    /// </summary>
    /// <returns>Returns an IEnumerable of the currently stored vote blocks.</returns>
    IEnumerable<VoteBlock> GetAllVotes();
    /// <summary>
    /// Get a list of all known voters.
    /// </summary>
    /// <returns>Returns an IEnumerable of the registered reference voters.</returns>
    IEnumerable<Origin> GetAllVoters();
    /// <summary>
    /// Gets all voters that are supporting the specified vote.
    /// </summary>
    /// <param name="vote">The vote to check on.</param>
    /// <returns>Returns an IEnumerable of the voter names that are supporting the given vote.</returns>
    IEnumerable<Origin> GetVotersFor(VoteBlock vote);
    /// <summary>
    /// Gets all user voters that are supporting the specified vote.
    /// </summary>
    /// <param name="vote">The vote to check on.</param>
    /// <returns>Returns an IEnumerable of the user voter names that are supporting the given vote.</returns>
    IEnumerable<Origin> GetUserVotersFor(VoteBlock vote);
    /// <summary>
    /// Gets a count of the known voters.
    /// </summary>
    /// <returns>Returns a count of the registered reference voters.</returns>
    int GetTotalVoterCount();

    bool HasVotes { get; }

    /// <summary>
    /// Determines if there is a more recent vote made by the author of the provided post.
    /// </summary>
    /// <param name="post">The post made by some author.</param>
    /// <returns>Returns true if there is a more recent post made by the author of the post.</returns>
    bool HasNewerVote(PostToProcess post);


    /// <summary>
    /// Function to add the provided votes to the current vote stores.
    /// </summary>
    /// <param name="votePartitions">The vote blocks to be added.</param>
    /// <param name="voter">The voter.</param>
    void AddVotes(IEnumerable<VoteBlock> votePartitions, Origin voter);
    /// <summary>
    /// Merge the vote supporters from one vote into another.
    /// </summary>
    /// <param name="fromVote">The originating vote.</param>
    /// <param name="toVote">The destination vote.</param>
    /// <returns>Returns true if successfully completed.</returns>
    bool Merge(VoteBlock fromVote, VoteBlock toVote);
    /// <summary>
    /// Transfer the vote supporters from one vote into several other votes.
    /// </summary>
    /// <param name="fromVote">The originating vote.</param>
    /// <param name="toVotes">The destination votes.</param>
    /// <returns>Returns true if successfully completed.</returns>
    bool Split(VoteBlock fromVote, IEnumerable<VoteBlock> toVotes);
    /// <summary>
    /// Shift support by various voters from their original vote to any votes
    /// supported by a specified target voter.
    /// </summary>
    /// <param name="voters">The voters that will support the new voter.</param>
    /// <param name="voterToJoin">The voter to join.</param>
    /// <returns>Returns true if successfully completed.</returns>
    bool Join(List<Origin> voters, Origin voterToJoin);
    /// <summary>
    /// Delete an entire vote and all associated supporters.
    /// </summary>
    /// <param name="vote">The vote to delete.</param>
    /// <returns>Returns true if successfully completed.</returns>
    bool Delete(VoteBlock vote);
    /// <summary>
    /// Undo a prior Merge, Join, or Delete action.
    /// </summary>
    /// <returns>Returns true if successfully completed.</returns>
    bool Undo();
    /// <summary>
    /// Check whether there are any stored undo actions.
    /// </summary>
    bool HasUndoActions { get; }
    /// <summary>
    /// Run any stored merges on the current data.
    /// </summary>
    void RunMergeActions();

    /// <summary>
    /// Get the index ordering value of the provided task.
    /// </summary>
    /// <param name="task">The task to index.</param>
    /// <returns>The index number for ordering.</returns>
    int TaskListIndex(VoteTask task);
    /// <summary>
    /// Add a user-defined task to the current list of tasks.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <returns>Returns true if the task was added, or false if it already exists.</returns>
    bool AddUserDefinedTask(VoteTask task);
    /// <summary>
    /// Request an update of the current task list to include any user-defined tasks.
    /// Run because the task list is cleared before each running of the tally.
    /// </summary>
    void AddUserDefinedTasksToTaskList();
    /// <summary>
    /// Reset the tasks list order based on the ordering type provided.
    /// </summary>
    /// <param name="order">The type of ordering to perform on the task list.</param>
    void ResetTasksOrder(TasksOrdering order);

    /// <summary>
    /// Replace the task on the given vote block with the new specified task.
    /// </summary>
    /// <param name="vote">The vote whose task is being changed.</param>
    /// <param name="task">The new task to use.</param>
    /// <returns>Returns true if the task was successfully changed and the vote records updated.</returns>
    bool ReplaceTask(VoteBlock vote, VoteTask task);

    /// <summary>
    /// Replace the current list of tasks with the provided list.
    /// </summary>
    /// <param name="tasks"></param>
    void ReplaceTasks(IEnumerable<VoteTask> tasks);
}
