using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Options;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    public class VoteCounter : IVoteCounter
    {
        readonly ILogger<VoteCounter> logger;
        readonly IGlobalOptions globalOptions;

        public VoteCounter(IGlobalOptions globalOptions, ILogger<VoteCounter> logger)
        {
            this.logger = logger;
            this.globalOptions = globalOptions;
        }

        #region Data Collections
        // Public

        /// <summary>
        /// The overall collection of voters and supporters.
        /// </summary>
        public VoteStorage VoteStorage { get; } = new VoteStorage();
        /// <summary>
        /// The list of posts that reference future posts, preventing immediate tallying.
        /// </summary>
        public HashSet<Post> FutureReferences { get; } = new HashSet<Post>();
        /// <summary>
        /// The list of posts collected from the quest. Read-only.
        /// </summary>
        public IReadOnlyList<Post> Posts => postsList;

        // Private

        readonly List<Post> postsList = new List<Post>();
        bool voteCounterIsTallying = false;

        Stack<UndoAction> UndoBuffer { get; } = new Stack<UndoAction>();
        MergeRecords UserMerges { get; } = new MergeRecords();

        VoterStorage ReferencePlans { get; } = new VoterStorage();
        HashSet<Origin> ReferenceOrigins { get; } = new HashSet<Origin>();
        #endregion

        #region General Tally Properties
        /// <summary>
        /// The quest the vote counter is set to track.
        /// </summary>
        public IQuest? Quest { get; set; } = null;

        /// <summary>
        /// The titles of the quest threads that have been tallied.
        /// </summary>
        public List<string> Titles { get; } = new List<string>();

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool VoteCounterIsTallying
        {
            get { return voteCounterIsTallying; }
            set
            {
                if (voteCounterIsTallying != value)
                {
                    voteCounterIsTallying = value;
                    OnPropertyChanged("Votes");
                    OnPropertyChanged("Voters");
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Track whether a tally was cancelled.
        /// </summary>
        public bool TallyWasCanceled { get; set; }

        /// <summary>
        /// Check whether there are any stored undo actions.
        /// </summary>
        public bool HasUndoActions => UndoBuffer.Count > 0;
        #endregion

        #region Reset various storage
        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        public void Reset()
        {
            VoteStorage.Clear();
            ReferenceOrigins.Clear();
            ReferencePlans.Clear();
            FutureReferences.Clear();
            UndoBuffer.Clear();

            VoteDefinedTasks.Clear();
            OrderedVoteTaskList.Clear();
            TaskList.Clear();

            OnPropertyChanged("VoteCounter");
            OnPropertyChanged("Tasks");
        }

        /// <summary>
        /// Reset user-defined tasks and user merges if the specified
        /// quest name is different than the one the vote counter has.
        /// </summary>
        /// <param name="forQuestName">The quest name that may have changed.</param>
        public void ResetUserDefinedTasks(string forQuestName)
        {
            if (Quest == null || Quest.DisplayName != forQuestName)
            {
                UserDefinedTasks.Clear();
                OrderedUserTaskList.Clear();
                ResetUserMerges();
            }
        }

        /// <summary>
        /// Reset any merges the user has made.
        /// </summary>
        public void ResetUserMerges()
        {
            UserMerges.Reset();
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
        #endregion

        #region Handling Posts
        /// <summary>
        /// Add a new set of posts for the <see cref="IVoteCounter"/> to use.
        /// </summary>
        /// <param name="posts">The posts to be stored in the <see cref="IVoteCounter"/>.</param>
        public void AddPosts(IEnumerable<Post> posts)
        {
            logger.LogDebug($"Adding {posts.Count()} posts to the VoteCounter.");

            postsList.Clear();
            if (posts != null)
                postsList.AddRange(posts);
        }

        /// <summary>
        /// Request that the currently stored posts be cleared.
        /// </summary>
        public void ClearPosts()
        {
            logger.LogDebug($"Clearing posts from the VoteCounter.");
            postsList.Clear();
        }
        #endregion

        #region Plan and Voter References
        /// <summary>
        /// Store a plan's information to allow it to be looked up by plan name or post ID.
        /// If the plan name has already been entered, will not update anything and return false.
        /// </summary>
        /// <param name="planName">The canonical name of the plan.</param>
        /// <param name="postID">The post ID the plan was defined in.</param>
        /// <param name="plan">The the vote line block that defines the plan.</param>
        /// <returns>Returns true if it was added, or false if it already exists.</returns>
        public bool AddReferencePlan(Origin planOrigin, VoteLineBlock plan)
        {
            // If it doesn't exist, we can just add it.
            if (ReferenceOrigins.Add(planOrigin))
            {
                ReferencePlans.Add(planOrigin, plan);
                return true;
            }
            else if (
                      (globalOptions.AllowUsersToUpdatePlans == BoolEx.True ||
                       (globalOptions.AllowUsersToUpdatePlans == BoolEx.Unknown && Quest!.AllowUsersToUpdatePlans)) &&
                      ReferenceOrigins.TryGetValue(planOrigin, out Origin currentOrigin)
                    )
            {
                // Author can replace existing version of a plan he wrote on conditions:
                // - Options allow plan replacement
                // - Plan written by same author
                // - Plan has the same name (surrounding if check, which includes identity type)
                // - New plan is in a later post than the previous
                // - New plan is more than one line (ie: not simply re-voting for the existing version)
                // - Content of the plan is different

                if (planOrigin.Source != Origin.Empty && planOrigin.Source == currentOrigin.Source &&
                    planOrigin.ID > currentOrigin.ID &&
                    plan.Lines.Count > 1 && 
                    ReferencePlans.TryGetValue(currentOrigin, out VoteLineBlock currentPlan) &&
                    plan != currentPlan)
                {
                    ReferenceOrigins.Remove(currentOrigin);
                    ReferenceOrigins.Add(planOrigin);
                    ReferencePlans[planOrigin] = plan;
                    return true;
                }
            }

            return false;
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

        /// <summary>
        /// Add a post to a store of future references made.
        /// </summary>
        /// <param name="post">The post to store.</param>
        /// <returns>Returns true if the post was added, or false if it already exists.</returns>
        public bool AddFutureReference(Post post)
        {
            return FutureReferences.Add(post);
        }
        #endregion

        #region Get Reference Data using strings
        /// <summary>
        /// Get canonical version of the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan being checked for.</param>
        /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
        public Origin? GetPlanOriginByName(string? planName)
        {
            if (string.IsNullOrEmpty(planName))
                return null;

            Origin test = new Origin(planName, IdentityType.Plan);

            if (ReferenceOrigins.TryGetValue(test, out Origin actual))
            {
                return actual;
            }

            return null;
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

            Origin test = new Origin(voterName, IdentityType.User);

            if (ReferenceOrigins.TryGetValue(test, out Origin actual))
            {
                return actual;
            }

            return null;
        }

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

        /// <summary>
        /// Determines if the specified voter is in the list of all possible voters for the tally.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns true if the voter has voted in the current tally.</returns>
        public bool HasVoter(string? voterName)
        {
            return GetVoterOriginByName(voterName) != null;
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
            if (ReferenceOrigins.TryGetValue(voter, out Origin actual))
            {
                return actual.ID;
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
        public Post? GetLastPostByAuthor(Origin author, PostId maxPostId)
        {
            if (ReferenceOrigins.TryGetValue(author, out Origin actual))
            {
                return postsList.Where(p => author == p.Origin &&
                                            (maxPostId == 0 || p.Origin.ID < maxPostId))
                                .MaxObject(p => p.Origin.ID);
            }

            return null;
        }

        /// <summary>
        /// Get the reference plan corresponding to the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan to get.</param>
        /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
        public VoteLineBlock? GetReferencePlan(Origin planOrigin)
        {
            return ReferencePlans.GetValueOrDefault(planOrigin);
        }

        /// <summary>
        /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
        /// </summary>
        /// <param name="voterName">The name of the voter or plan being requested.</param>
        /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
        public List<VoteLineBlock> GetVotesBy(Origin voter) => VoteStorage.GetVotesBy(voter);

        /// <summary>
        /// Gets a count of the known voters.
        /// </summary>
        /// <returns>Returns a count of the registered reference voters.</returns>
        public int GetTotalVoterCount()
        {
            return ReferenceOrigins.Count(o => o.AuthorType == IdentityType.User);
        }

        /// <summary>
        /// Get a collection of all the votes that currently have supporters.
        /// </summary>
        /// <returns>Returns an IEnumerable of the currently stored vote blocks.</returns>
        public IEnumerable<VoteLineBlock> GetAllVotes() => VoteStorage.GetAllVotes();

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
        public IEnumerable<Origin> GetVotersFor(VoteLineBlock vote) => VoteStorage.GetVotersFor(vote);
        #endregion

        #region Query if counter Has ...
        /// <summary>
        /// Determines whether the author of the provided post has made a newer vote submission.
        /// </summary>
        /// <param name="post">The post being checked.</param>
        /// <returns>Returns true if the voter has a newer vote already submitted.</returns>
        public bool HasNewerVote(Post post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (!HasVoter(post.Origin.Author))
                return false;

            return Posts.Any(p => p.Processed && p.Origin.Author == post.Origin.Author && p.Origin.ID > post.Origin.ID);
        }
        #endregion

        #region Adding / Modifying / Deleting Votes

        /// <summary>
        /// Add a collection of votes to the vote counter.
        /// </summary>
        /// <param name="votePartitions">A string list of all the parts of the vote to be added.</param>
        /// <param name="voter">The voter for this vote.</param>
        /// <param name="postID">The post ID for this vote.</param>
        /// <param name="voteType">The type of vote being added.</param>
        public void AddVotes(IEnumerable<VoteLineBlock> votePartitions, Origin voter)
        {
            if (!votePartitions.Any())
                return;

            // Remove the voter from any existing votes
            VoteStorage.RemoveVoterFromVotes(voter);

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
        public bool Merge(VoteLineBlock fromVote, VoteLineBlock toVote)
        {
            UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteStorage));
            UserMerges.AddMergeRecord(fromVote, toVote, UndoActionType.Merge, Quest!.PartitionMode);

            bool merged = MergeImplWrapper(fromVote, toVote);

            if (merged)
            {
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
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
        private bool MergeImplWrapper(VoteLineBlock fromVote, VoteLineBlock toVote)
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
            MergeImpl(fromVote, toVote, fromSupport, toSupport);

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
        private bool MergeImpl(VoteLineBlock fromVote, VoteLineBlock toVote,
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
        public bool Split(VoteLineBlock fromVote, List<VoteLineBlock> toVotes)
        {
            UndoBuffer.Push(new UndoAction(UndoActionType.Split, VoteStorage));
            UserMerges.AddMergeRecord(fromVote, toVotes, UndoActionType.Split, Quest!.PartitionMode);

            bool merged = SplitImplWrapper(fromVote, toVotes);

            if (merged)
            {
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
                OnPropertyChanged(nameof(HasUndoActions));
            }
            else
            {
                UndoBuffer.Pop();
            }

            return merged;
        }

        private bool SplitImplWrapper(VoteLineBlock fromVote, List<VoteLineBlock> toVotes)
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

                MergeImpl(fromVote, toVote, fromSupport, toSupport);
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
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
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
        public bool Delete(VoteLineBlock vote)
        {
            bool removed = false;

            if (VoteStorage.ContainsKey(vote))
            {
                UndoBuffer.Push(new UndoAction(UndoActionType.Delete, VoteStorage));

                removed = VoteStorage.Remove(vote);
            }

            if (removed)
            {
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
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

            if (Quest is null)
                throw new InvalidOperationException("Quest is null.");

            var undo = UndoBuffer.Pop();

            UserMerges.RemoveLastMergeRecord(Quest.PartitionMode, undo.ActionType);

            if (undo.Undo(this))
            {
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
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
            if (Quest != null)
            {
                var recordedMerges = UserMerges.GetMergeRecordList(Quest.PartitionMode);

                foreach (var mergeData in recordedMerges)
                {
                    UndoBuffer.Push(new UndoAction(mergeData.UndoActionType, VoteStorage));

                    if (mergeData.UndoActionType == UndoActionType.Split && mergeData.ToVotes.Count > 0)
                    {
                        SplitImplWrapper(mergeData.FromVote, mergeData.ToVotes);
                    }
                    else
                    {
                        MergeImplWrapper(mergeData.FromVote, mergeData.ToVote);
                    }
                }
            }
        }

        #endregion

        #region Task properties
        HashSet<string> VoteDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> UserDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> OrderedVoteTaskList { get; } = new List<string>();
        List<string> OrderedUserTaskList { get; } = new List<string>();
        public ObservableCollectionExt<string> TaskList { get; } = new ObservableCollectionExt<string>();


        /// <summary>
        /// Add tasks as we add votes.  If we register a new vote-defined task, add it
        /// to the ordered task lists.
        /// </summary>
        /// <param name="task">The new task to add to the knowledge base.</param>
        private void AddPotentialVoteTask(string task)
        {
            if (string.IsNullOrEmpty(task))
                return;

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
        /// Add a new user-defined vote.
        /// </summary>
        /// <param name="task">The task to add.</param>
        /// <returns>Returns true if the task was added to the knowledge base.</returns>
        public bool AddUserDefinedTask(string task)
        {
            if (string.IsNullOrEmpty(task))
                return false;

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

        /// <summary>
        /// Replace the task on the provided vote with the requested task.
        /// </summary>
        /// <param name="vote">The vote to update the task on.</param>
        /// <param name="task">The new task label.</param>
        /// <returns>Returns true if the task was updated.</returns>
        public bool ReplaceTask(VoteLineBlock vote, string task)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(vote.Task, task))
            {
                return false;
            }

            if (VoteStorage.TryGetValue(vote, out var supporters))
            {
                UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteStorage, vote));

                VoteStorage.Remove(vote);

                VoteLineBlock originalVote = vote.Clone();
                string originalTask = vote.Task;
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

                        UndoBuffer.Pop();

                        return false;
                    }
                }
                // If there's no conflict, update the tasks in the supporter votes and add the revised vote.
                else
                {
                    foreach (var (supporter, supporterVote) in supporters)
                    {
                        supporterVote.Task = task;
                    }

                    VoteStorage.Add(vote, supporters);
                }

                UserMerges.AddMergeRecord(originalVote, vote, UndoActionType.Other, Quest!.PartitionMode);

                OnPropertyChanged("Votes");
                OnPropertyChanged(nameof(HasUndoActions));
                return true;
            }

            return false;
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
}
