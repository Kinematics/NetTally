using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Collections;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Tally.ComponentsF.Storage;
using NetTally.VoteCounting;

namespace NetTally.Tally.ComponentsF.Counting;

/// <summary>
/// Class for managing and tracking votes and voters for a quest.
/// </summary>
/// <param name="globalOptions">Global program options.</param>
/// <param name="logger">Class logger.</param>
public class VoteCounterF(
    IOptions<GlobalSettings> globalOptions,
    ILogger<VoteCounter> logger) : IVoteCounterF
{
    private readonly GlobalSettings globalSettings = globalOptions.Value;
    private readonly ILogger<VoteCounter> logger = logger;

    #region Data Collections
    /// <summary>
    /// The list of posts collected from the quest. Read-only.
    /// </summary>
    private List<PostType> RawPosts { get; } = [];
    public List<PostToProcess> Posts { get; } = [];

    /// <summary>
    /// The overall collection of voters and supporters.
    /// </summary>
    public VoteStorage VoteStorage { get; } = [];

    VoterStorage ReferencePlans { get; } = [];

    HashSet<OriginType> ReferenceOrigins { get; } = new HashSet<OriginType>(OriginComparer.Instance);

    Stack<Storage.UndoAction> UndoBuffer { get; } = new();

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
    [AllowNull]
    public Quest Quest { get; set; } = null;

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
    public void AddPosts(IEnumerable<PostType> posts)
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
    public void SetThreadTitles(IEnumerable<string> titles)
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
    /// <param name="planName">The canonical name of the plan.</param>
    /// <param name="postID">The post ID the plan was defined in.</param>
    /// <param name="plan">The the vote line block that defines the plan.</param>
    /// <returns>Returns true if it was added, or false if it already exists.</returns>
    public bool AddReferencePlan(OriginType updateOrigin, VoteBlockType plan)
    {
        // If it doesn't exist, we can just add it.
        if (ReferenceOrigins.Add(updateOrigin))
        {
            ReferencePlans.Add(updateOrigin, plan);
            return true;
        }
        else if (CanUpdatePlans() &&
                 ReferenceOrigins.TryGetValue(updateOrigin, out OriginType? currentOrigin))
        {
            // Author can replace existing version of a plan he wrote on conditions:
            // - Options allow plan replacement
            // - Plan written by same author
            // - Plan has the same name (surrounding if check, which includes identity type)
            // - New plan is in a later post than the previous
            // - New plan is more than one line (ie: not simply re-voting for the existing version)
            // - Content of the plan is different

            if (updateOrigin.Source != Origin.None &&
                OriginComparer.Instance.Equals(updateOrigin.Source, currentOrigin.Source) &&
                PostIdComparer.Instance.Compare(updateOrigin.PostId, currentOrigin.PostId) == 1 &&
                plan.Lines.Count > 1 &&
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
    public bool AddReferenceVoter(OriginType voter)
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

    public bool HasPlan(AuthorType planAuthor)
    {
        return GetOriginByAuthor(planAuthor, IdentityType.Plan) != null;
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

    public bool HasVoter(AuthorType planAuthor)
    {
        return GetOriginByAuthor(planAuthor, IdentityType.User) != null;
    }

    /// <summary>
    /// Get canonical version of the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    public OriginType? GetPlanOriginByName(string? planName)
    {
        if (string.IsNullOrEmpty(planName))
            return null;

        var author = Author.Create(planName);

        return GetOriginByAuthor(author, IdentityType.Plan);
    }

    /// <summary>
    /// Get canonical version of the provided voter name.
    /// </summary>
    /// <param name="voterName">The name of the voter being checked for.</param>
    /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
    public OriginType? GetVoterOriginByName(string? voterName)
    {
        if (string.IsNullOrEmpty(voterName))
            return null;

        var author = Author.Create(voterName);

        return GetOriginByAuthor(author, IdentityType.User);
    }

    private OriginType? GetOriginByAuthor(AuthorType author, IdentityType identityType)
    {
        var namedOrigin = Origin.CreateOriginForName(identityType, author);

        if (namedOrigin == null)
            return null;

        return GetReferenceOrigin(namedOrigin);
    }

    private OriginType? GetReferenceOrigin(OriginType namedOrigin)
    {
        if (ReferenceOrigins.TryGetValue(namedOrigin, out OriginType? actualOrigin))
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
    public PostIdType? GetLatestVoterPostId(OriginType voter)
    {
        if (ReferenceOrigins.TryGetValue(voter, out OriginType? actual))
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
    public PostToProcess? GetLastPostByAuthor(OriginType author, PostIdType maxPostId)
    {
        var actualOrigin = GetReferenceOrigin(author);

        if (actualOrigin != null)
        {
            return Posts
                .Where(p => AuthorComparer.Instance.Equals(actualOrigin.Author, p.Origin.Author) &&
                            (maxPostId == PostId.Zero || PostIdComparer.Instance.Compare(p.Origin.PostId, maxPostId) < 0))
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
        if (!HasVoter(post.Origin.Author))
            return false;

        return Posts.Any(p =>
                           p.Processed
                           && PostIdComparer.Instance.Compare(p.Origin.PostId, post.Origin.PostId) > 1
                           && AuthorComparer.Instance.Equals(p.Origin.Author, post.Origin.Author));
    }

    /// <summary>
    /// Get the reference plan corresponding to the provided plan name.
    /// </summary>
    /// <param name="planName">The name of the plan to get.</param>
    /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
    public VoteBlockType? GetReferencePlan(OriginType planOrigin)
    {
        return ReferencePlans.GetValueOrDefault(planOrigin);
    }

    /// <summary>
    /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
    /// </summary>
    /// <param name="voterName">The name of the voter or plan being requested.</param>
    /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
    public IEnumerable<VoteBlockType> GetVotesBy(OriginType voter) => VoteStorage.GetVotesBy(voter);

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
    public IEnumerable<OriginType> GetAllVoters() => VoteStorage.GetAllVoters();

    /// <summary>
    /// Gets all voters that are supporting the specified vote.
    /// </summary>
    /// <param name="vote">The vote to check on.</param>
    /// <returns>Returns an IEnumerable of the voter names that are supporting the given vote.</returns>
    public IEnumerable<OriginType> GetVotersFor(VoteBlockType vote) => VoteStorage.GetVotersFor(vote);
    #endregion

    #region Adding / Modifying / Deleting Votes

    /// <summary>
    /// Add a collection of votes by a given voter to the vote counter.
    /// </summary>
    /// <param name="votePartitions">A string list of all the parts of the vote to be added.</param>
    /// <param name="voter">The voter for this vote.</param>
    public void AddVotes(List<VoteBlockType> votePartitions, OriginType voter)
    {
        if (votePartitions.Count == 0)
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
                var newVote = toVote.WithMarker(oldVote.Marker, oldVote.MarkerType, oldVote.MarkerValue);
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
    public bool Split(VoteBlockType fromVote, List<VoteBlockType> toVotes)
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

    private bool SplitImplWrapper(VoteBlockType fromVote, List<VoteBlockType> toVotes)
    {
        if (!VoteStorage.TryGetValue(fromVote, out var fromSupport))
        {
            return false;
        }

        foreach (var toVote in toVotes)
        {
            if (!VoteStorage.TryGetValue(toVote, out var toSupport))
            {
                return false;
            }

            MergeImpl(toVote, fromSupport, toSupport);
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
    public bool Join(List<OriginType> voters, OriginType voterToJoin)
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
        bool JoinImpl(OriginType joiningVoter, OriginType voterToJoin)
        {
            var source = GetVotesBy(joiningVoter);
            var dest = GetVotesBy(voterToJoin);

            if (source.Count == 0 || dest.Count == 0)
                return false;

            bool joined = false;

            // Remove support from any votes where the target voter isn't also present.
            foreach (var vote in source)
            {
                if (!VoteStorage.DoesVoterSupportVote(voterToJoin, vote))
                {
                    VoteStorage.RemoveSupporterFromVote(vote, joiningVoter);
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
    HashSet<VoteTaskType> VoteDefinedTasks { get; } = new(VoteTaskComparer.Instance);
    HashSet<VoteTaskType> UserDefinedTasks { get; } = new(VoteTaskComparer.Instance);
    List<VoteTaskType> OrderedVoteTaskList { get; } = [];
    List<VoteTaskType> OrderedUserTaskList { get; } = [];
    public ObservableCollectionExt<VoteTaskType> TaskList { get; } = [];


    /// <summary>
    /// Add tasks as we add votes.  If we register a new vote-defined task, add it
    /// to the ordered task lists.
    /// </summary>
    /// <param name="task">The new task to add to the knowledge base.</param>
    private void AddPotentialVoteTask(VoteTaskType task)
    {
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
    public bool AddUserDefinedTask(VoteTaskType task)
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

    public void ReplaceTasks(IEnumerable<VoteTaskType> tasks)
    {
        TaskList.Replace(tasks);
    }

    /// <summary>
    /// Replace the task on the provided vote with the requested task.
    /// </summary>
    /// <param name="vote">The vote to update the task on.</param>
    /// <param name="task">The new task label.</param>
    /// <returns>Returns true if the task was updated.</returns>
    public bool ReplaceTask(VoteBlockType vote, VoteTaskType task)
    {
        if (VoteTaskComparer.Instance.Equals(vote.Task, task))
        {
            return false;
        }

        UndoBuffer.Push(new Storage.UndoAction(UndoActionType.ReplaceTask, VoteStorage, vote));
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
    private bool ReplaceTaskImplWrapper(VoteBlockType vote, VoteTaskType task)
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
        vote.Task = task;

        // If there's a conflict with the newly-tasked vote, we need to merge with the existing vote.
        if (VoteStorage.ContainsKey(vote))
        {
            if (VoteStorage.TryGetValue(vote, out var toSupport))
            {
                foreach (var (supporterName, supporterVote) in supporters)
                {
                    if (!toSupport.ContainsKey(supporterName))
                    {
                        supporterVote.Task = task;
                        toSupport.Add(supporterName, supporterVote);
                    }
                }
            }
            else
            {
                // Undo the attempt if we couldn't get the conflicting vote data
                vote.Task = originalTask;

                VoteStorage.Add(vote, supporters);

                return false;
            }
        }
        // If there's no conflict, update the tasks in the supporter votes and add the revised vote.
        else
        {
            foreach (var (_, supporterVote) in supporters)
            {
                supporterVote.Task = task;
            }

            VoteStorage.Add(vote, supporters);
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
}
