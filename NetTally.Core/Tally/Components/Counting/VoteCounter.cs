using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Storage;
using NetTally.Tally.Components.Votes;
using NetTally.Utility.Collections;
using NetTally.Utility.Linq;

namespace NetTally.Tally.Components.Counting;

/// <summary>
/// Class for managing and tracking votes and voters for a quest.
/// </summary>
/// <param name="globalOptions">Global program options.</param>
/// <param name="logger">Class logger.</param>
public class VoteCounter(
    IOptions<GlobalSettings> globalOptions,
    ILogger<VoteCounter> logger,
    Quest quest) : IVoteCounter
{
    private readonly GlobalSettings globalSettings = globalOptions.Value;
    private readonly ILogger<VoteCounter> logger = logger;

    #region Data Collections
    /// <summary>
    /// The list of posts collected from the quest. Read-only.
    /// </summary>
    public List<PostToProcess> Posts { get; } = [];
    List<Post> RawPosts { get; } = [];

    /// <summary>
    /// The overall collection of voters and supporters.
    /// </summary>
    public VoteStorage VoteStorage { get; } = [];

    VoterStorage ReferencePlans { get; } = [];

    HashSet<Origin> ReferenceOrigins { get; } = new HashSet<Origin>(OriginNameComparer.Instance);

    Stack<UndoAction> UndoBuffer { get; } = new();

    MergeRecords UserMerges { get; } = new();
    #endregion

    #region State
    public bool HasPosts => RawPosts.Count > 0;
    public bool HasVotes => VoteStorage.Count > 0;
    public bool HasUndoActions => UndoBuffer.Count > 0;
    #endregion State

    #region Quest Information
    /// <summary>
    /// The quest the vote counter is set to track.
    /// </summary>
    public Quest Quest { get; set; } = quest;

    /// <summary>
    /// The titles of the quest threads that have been tallied.
    /// </summary>
    public List<string> Titles { get; } = [];
    #endregion Quest Information

    #region Reset various storage
    /// <summary>
    /// Reset all tracking variables.
    /// </summary>
    public void Reset()
    {
        VoteStorage.Clear();
        ReferenceOrigins.Clear();
        ReferencePlans.Clear();
        UndoBuffer.Clear();

        VoteDefinedTasks.Clear();
        OrderedVoteTaskList.Clear();
        TaskList.Clear();

        logger.LogDebug("Vote counter was reset.");
    }

    /// <summary>
    /// Reset user-defined tasks and user merges if the specified
    /// quest name is different than the one the vote counter has.
    /// </summary>
    public void ResetUserDefinedTasks()
    {
        UserDefinedTasks.Clear();
        OrderedUserTaskList.Clear();
        ResetUserMerges();
    }

    /// <summary>
    /// Reset any merges the user has made.
    /// </summary>
    public void ResetUserMerges()
    {
        UserMerges.Reset();
    }

    /// <summary>
    /// Request that the currently stored posts be cleared.
    /// </summary>
    public void ResetPosts()
    {
        RawPosts.Clear();
        Posts.Clear();
    }
    #endregion Reset various storage

    #region Load posts and titles
    /// <summary>
    /// Add a new set of posts for the <see cref="IVoteCounter"/> to use.
    /// </summary>
    /// <param name="posts">The posts to be stored in the <see cref="IVoteCounter"/>.</param>
    private void AddPosts(IEnumerable<Post> posts)
    {
        RawPosts.Clear();
        RawPosts.AddRange(posts);
        Posts.Clear();
        Posts.AddRange(posts.Select(p => new PostToProcess(p)));
    }

    /// <summary>
    /// Set the quest thread titles.
    /// </summary>
    /// <param name="titles">A list of titles to use.</param>
    private void SetThreadTitles(IEnumerable<string> titles)
    {
        Titles.Clear();
        Titles.AddRange(titles);
    }
    #endregion Load posts and titles

    #region Plan and Voter References
    /// <summary>
    /// Store a plan's information to allow it to be looked up by plan name or post ID.
    /// If the plan name has already been entered, will not update anything and return false.
    /// </summary>
    /// <param name="updateOrigin">The <see cref="Origin"/> key we are attempting to update.</param>
    /// <param name="plan">The vote block being updated/added.</param>
    /// <returns></returns>
    public bool AddReferencePlan(Origin updateOrigin, VoteBlockType plan)
    {
        // If it doesn't exist, we can just add it.
        if (ReferenceOrigins.Add(updateOrigin))
        {
            ReferencePlans.Add(updateOrigin, plan);
            return true;
        }
        else if (CanUpdatePlans() &&
                 ReferenceOrigins.TryGetValue(updateOrigin, out Origin? currentOrigin))
        {
            // Author can replace existing version of a plan he wrote on conditions:
            // - Options allow plan replacement
            // - Plan written by same author
            // - Plan has the same name (surrounding if check, which includes identity type)
            // - New plan is in a later post than the previous
            // - New plan is more than one line (ie: not simply re-voting for the existing version)
            // - Content of the plan is different

            if (updateOrigin.Source() != Origins.None &&
                OriginComparer.Instance.Equals(updateOrigin.Source(), currentOrigin.Source()) &&
                PostIdComparer.Instance.Compare(updateOrigin.PostId, currentOrigin.PostId) == 1 &&
                plan.LineCount > 1 &&
                ReferencePlans.TryGetValue(currentOrigin, out VoteBlockType? currentPlan) &&
                !VoteBlockComparer.Instance.Equals(plan, currentPlan))
            {
                ReferenceOrigins.Remove(currentOrigin);
                ReferenceOrigins.Add(updateOrigin);
                ReferencePlans[updateOrigin] = plan;
                return true;
            }
        }

        return false;
    }

    private bool CanUpdatePlans()
    {
        return globalSettings.AllowUsersToUpdatePlans == BoolEx.True ||
              globalSettings.AllowUsersToUpdatePlans == BoolEx.Unknown && Quest.AllowUsersToUpdatePlans;
    }

    /// <summary>
    /// Store a voter and their post ID.
    /// This is expecting to be called for every vote by the user,
    /// so the post ID will eventually be that user's last vote in the tally.
    /// </summary>
    /// <param name="voterName">The proper name of the voter.</param>
    /// <param name="postID">The ID of their vote post.</param>
    /// <returns>Returns true if the voter was added, or false if the voter already exists.</returns>
    public bool AddReferenceVoter(Origin voter)
    {
        return ReferenceOrigins.Add(voter);
    }
    #endregion

    #region Get Reference Data using strings
    /// <summary>
    /// Determine if the requested plan name exists in the current list of plans.
    /// Applies the plan name marker character to the provided plan name, if it
    /// doesn't already exist.
    /// </summary>
    /// <param name="planName">The name of the plan to check for.</param>
    /// <returns>Returns whether the provided plan name exists in the current PlanNames hash set.</returns>
    public bool HasPlan(string? planName)
    {
        return GetPlanOriginByName(planName) != null;
    }

    public bool HasPlan(Author planAuthor)
    {
        return GetOriginByPlanAuthor(planAuthor) != null;
    }

    /// <summary>
    /// Determines if the specified voter is in the list of all possible voters for the tally.
    /// </summary>
    /// <param name="voterName">The name of the voter to check for.</param>
    /// <returns>Returns true if the voter has voted in the current tally.</returns>
    public bool HasVoter(string? voterName)
    {
        return GetVoterOriginByName(voterName) != null;
    }

    public bool HasVoter(Author planAuthor)
    {
        return GetOriginByUserAuthor(planAuthor) != null;
    }

    /// <summary>
    /// Get canonical version of the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    public Origin? GetPlanOriginByName(string? planName)
    {
        if (string.IsNullOrEmpty(planName))
            return null;

        var author = Authors.Create(planName);

        return GetOriginByPlanAuthor(author);
    }

    /// <summary>
    /// Get canonical version of the provided voter name.
    /// </summary>
    /// <param name="voterName">The name of the voter being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    public Origin? GetVoterOriginByName(string? voterName)
    {
        if (string.IsNullOrEmpty(voterName))
            return null;

        var author = Authors.Create(voterName);

        return GetOriginByUserAuthor(author);
    }

    /// <summary>
    /// Gets an existing stored origin that matches the provided author.
    /// </summary>
    /// <param name="author">The author to query.</param>
    /// <param name="identityType">The identity type of the author.</param>
    /// <returns>The existing origin, if it exists, or null.</returns>
    private Origin? GetOriginByUserAuthor(Author author)
    {
        var namedOrigin = Origins.CreateUserNameOnly(author);

        return GetReferenceOrigin(namedOrigin);
    }

    private Origin? GetOriginByPlanAuthor(Author author)
    {
        var namedOrigin = Origins.CreatePlanNameOnly(author);

        return GetReferenceOrigin(namedOrigin);
    }

    /// <summary>
    /// Gets and existing stored origin that matches the provided origin.
    /// </summary>
    /// <param name="namedOrigin">An origin with a named author, either user or plan.</param>
    /// <returns>An origin stored in our reference pool, if found. Otherwise null.</returns>
    private Origin? GetReferenceOrigin(Origin namedOrigin)
    {
        if (ReferenceOrigins.TryGetValue(namedOrigin, out Origin? actualOrigin))
        {
            return actualOrigin;
        }

        return null;
    }
    #endregion

    #region Get Reference Data
    /// <summary>
    /// Get the ID of the post by the specified author at the time of the request.
    /// This may change over the course of a tally.
    /// </summary>
    /// <param name="voterName">The name of the voter to check for.</param>
    /// <returns>Returns the post ID if the voter's most recently processed post, or 0 if not found.</returns>
    public PostId? GetLatestVoterPostId(Origin voter)
    {
        if (ReferenceOrigins.TryGetValue(voter, out Origin? actual))
        {
            return actual.PostId;
        }

        return null;
    }

    /// <summary>
    /// Get the last post made by a given author.
    /// Possibly restrict the search range to no more than the specified post ID.
    /// </summary>
    /// <param name="voterName">The voter being queried.</param>
    /// <param name="maxPostId">The highest post ID allowed. 0 means unrestricted.</param>
    /// <returns>Returns the last post by the requested author, if found. Otherwise null.</returns>
    public PostToProcess? GetLastPostByAuthor(Origin author, PostId maxPostId)
    {
        var actualOrigin = GetReferenceOrigin(author);

        if (actualOrigin != null)
        {
            return Posts
                .Where(p => AuthorComparer.Instance.Equals(actualOrigin.Author, p.Origin.Author) &&
                            (maxPostId == PostIds.Zero || PostIdComparer.Instance.Compare(p.Origin.PostId, maxPostId) < 0))
                .MaxBy(p => p.Origin.PostId, PostIdComparer.Instance);

        }

        return null;
    }

    /// <summary>
    /// Determines whether the author of the provided post has made a newer vote submission.
    /// </summary>
    /// <param name="post">The post being checked.</param>
    /// <returns>Returns true if the voter has a newer vote already submitted.</returns>
    public bool HasNewerVote(PostToProcess post)
    {
        return Posts
            .Any(p =>
                p.Processed &&
                OriginNameComparer.Instance.Equals(p.Origin, post.Origin) &&
                PostIdComparer.Instance.Compare(p.Origin.PostId, post.Origin.PostId) == 1);
    }

    /// <summary>
    /// Get the reference plan corresponding to the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan to get.</param>
    /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
    public VoteBlockType? GetReferencePlan(Origin planOrigin)
    {
        return ReferencePlans.GetValueOrDefault(planOrigin);
    }

    public IEnumerable<VoteBlockType> GetReferencePlans()
    {
        return ReferencePlans.Select(p => p.Value);
    }

    /// <summary>
    /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
    /// </summary>
    /// <param name="voterName">The name of the voter or plan being requested.</param>
    /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
    public IEnumerable<VoteBlockType> GetVotesBy(Origin voter) => VoteStorage.GetVotesBy(voter);

    /// <summary>
    /// Gets a count of the known voters.
    /// </summary>
    /// <returns>Returns a count of the registered reference voters.</returns>
    public int GetTotalVoterCount()
    {
        return ReferenceOrigins.Count(o => o.IsUser);
    }

    /// <summary>
    /// Get a collection of all the votes that currently have supporters.
    /// </summary>
    /// <returns>Returns an IEnumerable of the currently stored vote blocks.</returns>
    public IEnumerable<VoteBlockType> GetAllVotes() => VoteStorage.GetAllVotes();

    /// <summary>
    /// Get a list of all known voters.
    /// </summary>
    /// <returns>Returns an IEnumerable of the registered reference voters.</returns>
    public IEnumerable<Origin> GetAllVoters() => VoteStorage.GetAllVoters();

    /// <summary>
    /// Gets all voters that are supporting the specified vote.
    /// </summary>
    /// <param name="vote">The vote to check on.</param>
    /// <returns>Returns an IEnumerable of the voter names that are supporting the given vote.</returns>
    public IEnumerable<Origin> GetVotersFor(VoteBlockType vote) =>
        VoteStorage.GetVotersFor(vote);

    /// <summary>
    /// Gets all voters that are supporting the specified vote.
    /// </summary>
    /// <param name="vote">The vote to check on.</param>
    /// <returns>Returns an IEnumerable of the voter names that are supporting the given vote.</returns>
    public IEnumerable<Origin> GetUserVotersFor(VoteBlockType vote) =>
        VoteStorage.GetUserVotersFor(vote);
    #endregion

    #region Adding / Modifying / Deleting Votes

    /// <summary>
    /// Add a collection of votes by a given voter to the vote counter.
    /// </summary>
    /// <param name="votePartitions">A string list of all the parts of the vote to be added.</param>
    /// <param name="voter">The voter for this vote.</param>
    public void AddVotes(IEnumerable<VoteBlockType> votePartitions, Origin voter)
    {
        if (!votePartitions.Any())
            return;

        // Remove the voter from any existing votes
        VoteStorage.RemoveSupporterFromAllVotes(voter);

        // Add/update all segments of the provided vote
        foreach (var partition in votePartitions)
        {
            VoteStorage.AddSupporterToVote(partition, voter);
            AddPotentialVoteTask(partition.Task);
        }

        // Cleanup any votes that no longer have any support
        VoteStorage.RemoveUnsupportedVotes();
    }


    /// <summary>
    /// Merge the vote supporters from one vote into another.
    /// </summary>
    /// <param name="fromVote">The originating vote.</param>
    /// <param name="toVote">The destination vote.</param>
    /// <returns>Returns true if successfully completed.</returns>
    public bool Merge(VoteBlockType fromVote, VoteBlockType toVote)
    {
        UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteStorage));
        UserMerges.AddMergeRecord(fromVote, toVote, UndoActionType.Merge, Quest.PartitionMode);

        bool merged = MergeImplWrapper(fromVote, toVote);

        if (merged)
        {
            OnPropertyChanged(nameof(HasUndoActions));
        }
        else
        {
            UndoBuffer.Pop();
        }

        return merged;
    }

    /// <summary>
    /// The wrapper handles the process of extracting the vote support from
    /// the storage before passing the pieces on to the implementation.
    /// </summary>
    /// <param name="fromVote">The vote being merged.</param>
    /// <param name="toVote">The vote being merged into.</param>
    /// <returns>Returns true if there was a successful merge.</returns>
    private bool MergeImplWrapper(VoteBlockType fromVote, VoteBlockType toVote)
    {
        if (fromVote == toVote)
            return false;

        if (!VoteStorage.TryGetValue(fromVote, out var fromSupport))
        {
            return false;
        }

        if (!VoteStorage.TryGetValue(toVote, out var toSupport))
        {
            return false;
        }

        // Theoretically, all the supporters in the from vote could already
        // be in the to vote, in which case no merging would happen.
        MergeImpl(toVote, fromSupport, toSupport);

        // But we still want to remove the from vote.
        return VoteStorage.Remove(fromVote);
    }

    /// <summary>
    /// Implement the logic for combining two support blocks of voters.
    /// </summary>
    /// <param name="fromVote">The vote being merged from.</param>
    /// <param name="toVote">The vote being merged into.</param>
    /// <param name="fromSupport">The support block for the from vote.</param>
    /// <param name="toSupport">The support block for the to vote.</param>
    /// <returns>Returns true if any supporters were successfully added to the to block.</returns>
    private static bool MergeImpl(VoteBlockType toVote,
        VoterStorage fromSupport, VoterStorage toSupport)
    {
        bool merged = false;

        foreach (var (supporterName, oldVote) in fromSupport)
        {
            if (!toSupport.ContainsKey(supporterName))
            {
                var newVote = toVote with { Marker = oldVote.Marker };
                toSupport.Add(supporterName, newVote);
                merged = true;
            }
        }

        return merged;
    }


    /// <summary>
    /// Merge the vote supporters from one vote into several other votes.
    /// </summary>
    /// <param name="fromVote">The originating vote.</param>
    /// <param name="toVotes">The destination votes.</param>
    /// <returns>Returns true if successfully completed.</returns>
    public bool Split(VoteBlockType fromVote, IEnumerable<VoteBlockType> toVotes)
    {
        UndoBuffer.Push(new UndoAction(UndoActionType.Split, VoteStorage));
        UserMerges.AddMergeRecord(fromVote, toVotes, UndoActionType.Split, Quest.PartitionMode);

        bool merged = SplitImplWrapper(fromVote, toVotes);

        if (merged)
        {
            OnPropertyChanged(nameof(HasUndoActions));
        }
        else
        {
            UndoBuffer.Pop();
        }

        return merged;
    }

    private bool SplitImplWrapper(VoteBlockType fromVote, IEnumerable<VoteBlockType> toVotes)
    {
        if (!VoteStorage.TryGetValue(fromVote, out var fromSupport))
        {
            return false;
        }

        foreach (var toVote in toVotes)
        {
            toVote.Category = fromVote.Category;
            AddPotentialVoteTask(toVote.Task);

            if (!VoteStorage.TryGetValue(toVote, out var toSupport))
            {
                toSupport = [];
            }

            if (MergeImpl(toVote, fromSupport, toSupport))
            {
                VoteStorage.Add(toVote, toSupport);
            }
        }

        // But we still want to remove the from vote.
        return VoteStorage.Remove(fromVote);
    }

    /// <summary>
    /// Shift support by various voters from their original vote to any votes
    /// supported by a specified target voter.
    /// </summary>
    /// <param name="voters">The voters that will support the new voter.</param>
    /// <param name="voterToJoin">The voter to join.</param>
    /// <returns>Returns true if successfully completed.</returns>
    public bool Join(List<Origin> voters, Origin voterToJoin)
    {
        bool joined = false;

        UndoBuffer.Push(new UndoAction(UndoActionType.Join, VoteStorage));

        foreach (var voter in voters)
        {
            joined = JoinImpl(voter, voterToJoin) || joined;
        }

        if (joined)
        {
            OnPropertyChanged(nameof(HasUndoActions));
        }
        else
        {
            UndoBuffer.Pop();
        }

        return joined;

        /// <summary>
        /// Implement joining logic per voter.
        /// </summary>
        /// <param name="joiningVoter">The voter being moved to a new voting support set.</param>
        /// <param name="voterToJoin">The voter being joined.</param>
        /// <returns>Returns true if the join was completed.</returns>
        bool JoinImpl(Origin joiningVoter, Origin voterToJoin)
        {
            var source = GetVotesBy(joiningVoter);
            var dest = GetVotesBy(voterToJoin);

            if (!source.Any() || !dest.Any())
                return false;

            bool joined = false;

            // Remove support from any votes where the target voter isn't also present.
            foreach (var vote in source)
            {
                if (!VoteStorage.DoesVoterSupportVote(voterToJoin, vote))
                {
                    VoteStorage.RemoveSupporterFromVote(joiningVoter, vote);
                }
            }

            VoteStorage.RemoveUnsupportedVotes();

            foreach (var vote in dest)
            {
                if (!VoteStorage.DoesVoterSupportVote(joiningVoter, vote))
                {
                    VoteStorage.AddSupporterToVote(vote, joiningVoter);
                    joined = true;
                }
            }

            return joined;
        }
    }

    /// <summary>
    /// Delete an entire vote and all associated supporters.
    /// </summary>
    /// <param name="vote">The vote to delete.</param>
    /// <returns>Returns true if successfully completed.</returns>
    public bool Delete(VoteBlockType vote)
    {
        bool removed = false;

        if (VoteStorage.ContainsKey(vote))
        {
            UndoBuffer.Push(new UndoAction(UndoActionType.Delete, VoteStorage));

            removed = VoteStorage.Remove(vote);
        }

        if (removed)
        {
            OnPropertyChanged(nameof(HasUndoActions));
        }
        else
        {
            UndoBuffer.Pop();
        }

        return removed;
    }

    /// <summary>
    /// Undoes the most recently performed modification to the vote count.
    /// </summary>
    /// <returns>Returns true if it performed an undo action.  Otherwise, false.</returns>
    public bool Undo()
    {
        if (!HasUndoActions)
            return false;

        UndoAction undoAction = UndoBuffer.Pop();

        UserMerges.RemoveLastMergeRecord(Quest.PartitionMode, undoAction.ActionType);

        if (undoAction.Undo(this))
        {
            OnPropertyChanged(nameof(HasUndoActions));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Run any stored merges on the current data.
    /// </summary>
    public void RunMergeActions()
    {
        var recordedMerges = UserMerges.GetMergeRecordList(Quest.PartitionMode);

        foreach (var mergeData in recordedMerges)
        {
            if (mergeData.UndoActionType == UndoActionType.ReplaceTask)
            {
                UndoBuffer.Push(new UndoAction(mergeData.UndoActionType, VoteStorage, storageVote: mergeData.FromVote));
            }
            else
            {
                UndoBuffer.Push(new UndoAction(mergeData.UndoActionType, VoteStorage));
            }

            if (mergeData.UndoActionType == UndoActionType.Split && mergeData.ToVotes.Count > 0)
            {
                SplitImplWrapper(mergeData.FromVote, mergeData.ToVotes);
            }
            else if (mergeData.UndoActionType == UndoActionType.ReplaceTask)
            {
                ReplaceTaskImplWrapper(mergeData.FromVote, mergeData.ToVote.Task);
            }
            else
            {
                MergeImplWrapper(mergeData.FromVote, mergeData.ToVote);
            }
        }
    }
    #endregion

    #region Task properties
    HashSet<VoteTask> VoteDefinedTasks { get; } = new(VoteTaskComparer.Instance);
    HashSet<VoteTask> UserDefinedTasks { get; } = new(VoteTaskComparer.Instance);
    List<VoteTask> OrderedVoteTaskList { get; } = [];
    List<VoteTask> OrderedUserTaskList { get; } = [];
    public ObservableCollectionExt<VoteTask> TaskList { get; } = [];

    /// <summary>
    /// Get the index ordering value of the provided task.
    /// </summary>
    /// <param name="task">The task to index.</param>
    /// <returns>The index number for ordering.</returns>
    public int TaskListIndex(VoteTask task)
    {
        if (task == VoteTask.Empty)
            return -99;

        for (int i = 0; i < TaskList.Count; i++)
        {
            if (VoteTaskComparer.Instance.Equals(TaskList[i], task))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Add tasks as we add votes.  If we register a new vote-defined task, add it
    /// to the ordered task lists.
    /// </summary>
    /// <param name="task">The new task to add to the knowledge base.</param>
    private void AddPotentialVoteTask(VoteTask task)
    {
        if (task == VoteTask.Empty)
        {
            return;
        }

        if (!UserDefinedTasks.Contains(task))
        {
            if (VoteDefinedTasks.Add(task))
            {
                OrderedVoteTaskList.Add(task);
                TaskList.Add(task);
                OnPropertyChanged("Tasks");
            }
        }
    }

    /// <summary>
    /// Add a new user-defined task.
    /// </summary>
    /// <param name="task">The task to add.</param>
    /// <returns>Returns true if the task was added to the knowledge base.</returns>
    public bool AddUserDefinedTask(VoteTask task)
    {
        if (UserDefinedTasks.Add(task))
        {
            OrderedUserTaskList.Add(task);
            TaskList.Add(task);
            OnPropertyChanged("Tasks");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Update the general task list with any user-defined 
    /// tasks at the end of a tally.
    /// </summary>
    public void AddUserDefinedTasksToTaskList()
    {
        TaskList.AddRange(OrderedUserTaskList);

        OnPropertyChanged("Tasks");
    }

    /// <summary>
    /// Resets the tasks order.
    /// </summary>
    /// <param name="order">The type of ordering to use.</param>
    public void ResetTasksOrder(TasksOrdering order)
    {
        if (order == TasksOrdering.Alphabetical)
        {
            TaskList.Sort();
        }
        else if (order == TasksOrdering.AsTallied)
        {
            TaskList.Clear();
            TaskList.AddRange(OrderedVoteTaskList.Concat(OrderedUserTaskList));
        }

        OnPropertyChanged("Tasks");
    }

    public void ReplaceTasks(IEnumerable<VoteTask> tasks)
    {
        TaskList.Replace(tasks);
    }

    /// <summary>
    /// Replace the task on the provided vote with the requested task.
    /// </summary>
    /// <param name="vote">The vote to update the task on.</param>
    /// <param name="task">The new task label.</param>
    /// <returns>Returns true if the task was updated.</returns>
    public bool ReplaceTask(VoteBlockType vote, VoteTask task)
    {
        if (VoteTaskComparer.Instance.Equals(vote.Task, task))
        {
            return false;
        }

        UndoBuffer.Push(new UndoAction(UndoActionType.ReplaceTask, VoteStorage, vote));
        VoteBlockType originalVote = VoteBlock.Clone(vote);

        if (ReplaceTaskImplWrapper(vote, task))
        {
            UserMerges.AddMergeRecord(originalVote, vote, UndoActionType.ReplaceTask, Quest.PartitionMode);

            OnPropertyChanged("Votes");
            OnPropertyChanged(nameof(HasUndoActions));
            return true;
        }

        UndoBuffer.Pop();
        return false;
    }

    /// <summary>
    /// Wrapper for handling replace task, but without adding to merge records.
    /// </summary>
    /// <param name="vote">The vote being modified.</param>
    /// <param name="task">The new task to apply to the vote.</param>
    /// <returns>Returns true if the task replacement was successfully completed.</returns>
    private bool ReplaceTaskImplWrapper(VoteBlockType vote, VoteTask task)
    {
        if (!VoteStorage.TryGetValue(vote, out var supporters))
        {
            return false;
        }

        // Incoming parameter may be an entry in storage, or a copy of a vote.
        // Adjust so that we're always pointing at an actual vote.
        // If the vote isn't found in VoteStorage, just use the one provided.
        vote = VoteStorage.GetVoteMatching(vote) ?? vote;

        // Remove the version of the vote we're starting with.
        VoteStorage.Remove(vote);

        var originalTask = vote.Task;
        var replacementVote = vote with { Task = task };

        // If there's a conflict with the newly-tasked vote, we need to merge with the existing vote.
        if (VoteStorage.TryGetValue(replacementVote, out var toSupport))
        {
            var updatedSupporters =
                supporters.Select(s => new VoterStorageEntryF(s.Key, s.Value with { Task = task }));

            foreach (var sup in updatedSupporters)
            {
                if (!toSupport.ContainsKey(sup.Key))
                {
                    toSupport.Add(sup.Key, sup.Value);
                }
            }
        }
        // If there's no conflict, update the tasks in the supporter votes and add the revised vote.
        else
        {
            var updatedSupporters = new VoterStorage(
                supporters.Select(s => new VoterStorageEntryF(s.Key, s.Value with { Task = task })));

            VoteStorage.Add(replacementVote, updatedSupporters);
        }

        return true;
    }

    #endregion

    #region INotifyPropertyChanged interface
    /// <summary>
    /// Event for INotifyPropertyChanged.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Function to raise events when a property has been changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that was modified.</param>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    #region Process Posts into Votes
    /// <summary>
    /// Construct votes from the provided posts.
    /// </summary>
    /// <param name="titles">The titles to display during output.</param>
    /// <param name="posts">The posts to be processed.</param>
    public void ConstructVotes(IEnumerable<string> titles, IEnumerable<Post> posts)
    {
        SetThreadTitles(titles);
        AddPosts(posts);
        ConstructVotes();
    }

    /// <summary>
    /// Construct votes from existing posts.
    /// </summary>
    public void ConstructVotes()
    {
        Reset();
        PreprocessPosts();
        ProcessPosts();
    }


    public static List<VoteBlockType> GetVoteBlocks(IEnumerable<VoteLineType> lines) =>
        [.. VoteBlocks.GetBlocks(lines)];

    public static List<VoteBlockType> GetVoteAsBlock(IEnumerable<VoteLineType> lines) =>
        [VoteBlock.Create(lines)!];

    private static Func<PostToProcess, List<VoteBlockType>> PostBlocks =>
        (p) => GetVoteBlocks(p.VoteLines);

    private static Func<PostToProcess, List<VoteBlockType>> PostAsBlock =>
        (p) => GetVoteAsBlock(p.VoteLines);

    // Either split the vote into blocks, or encapsulate the vote into an enumerable
    // so that it can be treated the same way.
    static readonly List<(Func<PostToProcess, List<VoteBlockType>> postToBlocks,
                          Func<VoteBlockType, PlanDescriptor> isPlanFunction)>
        planProcesses =
        [
            (postToBlocks: PostBlocks, isPlanFunction: VoteBlocks.IsBlockAProposedPlan),
            (postToBlocks: PostBlocks, isPlanFunction: VoteBlocks.IsBlockAnExplicitPlan),
            (postToBlocks: PostAsBlock, isPlanFunction: VoteBlocks.IsBlockAnImplicitPlan),
            (postToBlocks: PostAsBlock, isPlanFunction: VoteBlocks.IsBlockASingleLinePlan)
        ];

    /// <summary>
    /// Handle preprocessing of posts for a quest.
    /// </summary>
    private void PreprocessPosts()
    {
        Posts.WithEach(p => p.Reset())
             .WithEach(p => AddReferenceVoter(p.Origin));

        // Run the above series of preprocessing functions to extract plans from the post list.
        PreprocessPlans();
    }

    /// <summary>
    /// Run the logic for the sequence of preprocessing phases for plan examination and extraction.
    /// </summary>
    private void PreprocessPlans()
    {
        planProcesses.ForEach(pp =>
        {
            Posts.ForEach(p =>
            {
                VoteConstructor.PreprocessPostGetPlans(
                    Quest,
                    p.Origin.Author,
                    pp.isPlanFunction,
                    pp.postToBlocks(p))
                .Select(pl => NormalizePlan(pl.Key, pl.Value))
                .Where(a => a.HasValue)
                .Select(a => a!.Value)
                .Select(a => (a.Contents,
                              Origin: Origins.CreatePlan(p.Origin, Authors.Create(a.Name))))
                .Where(a => AddReferencePlan(a.Origin, a.Contents))
                .Select(a => (Partitions: VoteConstructor.PartitionPlan(a.Contents, Quest.PartitionMode),
                              a.Origin))
                .WithEach(a => AddVotes(a.Partitions, a.Origin));
            });
        });
    }

    private void ProcessPosts()
    {
        Posts.TryProcess(p => VP(p, Quest), p => ForceVP(p, Quest));
        AddUserDefinedTasksToTaskList();
        RunMergeActions();

        // Handle processing each post and adding votes if successful.
        bool VP(PostToProcess post, Quest quest)
        {
            if (VoteConstructor.TryProcessPostGetVotes(post, quest, out List<VoteBlockType> votes))
            {
                AddVotes(votes, post.Origin);
                return true;
            }

            return false;
        }

        // Handle processing votes if the processing loop failed.
        void ForceVP(PostToProcess post, Quest quest)
        {
            post.ForceProcess = true;
            VoteConstructor.TryProcessPostGetVotes(post, quest, out List<VoteBlockType> votes);
            AddVotes(votes, post.Origin);
        }
    }

    /// <summary>
    /// Given a plan block, if the plan is a base/proposed plan, rename it as just a "Plan".
    /// Convert the marker for all lines to None.
    /// </summary>
    /// <param name="plan">The plan to examine.</param>
    /// <returns>Returns the original plan, or the modified plan if it used "Base Plan".</returns>
    public (string Name, VoteBlockType Contents)?
        NormalizePlan(string originalPlanName, VoteBlockType originalVoteBlock)
    {
        if (originalVoteBlock.LineCount == 0 || string.IsNullOrEmpty(originalPlanName))
            return null;

        VoteLineType firstLine = originalVoteBlock.Lines[0] with { Marker = Marker.Empty };

        var (planType, planName) = VoteBlocks.CheckIfPlan(firstLine);

        if (planType == PlanStatus.None)
            return null;

        // Proposed plans need to be converted to an unadorned plan name.
        // Normal plans should be written to be consistent with that.
        var convertName = VoteContent.Create($"Plan: {planName}");

        if (convertName != null)
        {
            firstLine = firstLine with { Content = convertName };
        }

        // All vote lines in a plan should have MarkerType of None.
        // This allows them to be part of any comparison, and easily mesh with various output.
        var remainingLines = originalVoteBlock.Skip(1)
            .Select(v => v with { Marker = Marker.Empty });

        // Stack stuff back together
        List<VoteLineType> voteLines = [firstLine, .. remainingLines];

        var returnPlan = VoteBlock.Create(voteLines);

        if (returnPlan != null)
        {
            returnPlan = returnPlan with { Marker = Marker.PlanMarker };
            return (planName, returnPlan);
        }

        return null;
    }
    #endregion Process Posts into Votes

}
