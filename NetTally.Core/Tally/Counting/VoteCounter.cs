﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Utility;
using NetTally.Votes;
using NetTally.Experiment3;
using NetTally.Extensions;

namespace NetTally.VoteCounting
{
    public class VoteCounter : IVoteCounter
    {
        #region private collections
        readonly Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();
        readonly Dictionary<string, string> cleanedKeys = new Dictionary<string, string>();
        readonly MergeRecords userMerges = new MergeRecords();
        readonly List<Post> postsList = new List<Post>();
        readonly List<string> taskList = new List<string>();
        bool voteCounterIsTallying = false;

        Stack<UndoAction> UndoBuffer { get; } = new Stack<UndoAction>();

        HashSet<string> ReferencePlanNames { get; set; } = new HashSet<string>(Agnostic.StringComparer);
        HashSet<string> ReferenceVoterNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> PlanMessageIdEx3 { get; set; } = new Dictionary<string, int>(Agnostic.StringComparer);
        Dictionary<string, int> VoterReferenceMessageIdEx3 { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> VoterMessageIdEx3 { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, VoteLineBlock> ReferencePlansEx3 { get; set; } = new Dictionary<string, VoteLineBlock>(Agnostic.InsensitiveComparer);




        // Deprecated collections:
        HashSet<string> PlanNames { get; set; } = new HashSet<string>(Agnostic.StringComparer);
        Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, HashSet<string>> VotesWithSupporters { get; set; } = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);
        Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; set; } = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);

        #endregion


        #region General Tally Properties
        /// <summary>
        /// The quest the vote counter is set to track.
        /// </summary>
        public IQuest? Quest { get; set; } = null;

        /// <summary>
        /// The title of the quest thread when tallied.
        /// </summary>
        public string Title { get; set; } = string.Empty;

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




        public Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>> VoteBlockSupporters { get; private set; }
            = new Dictionary<VoteLineBlock, Dictionary<string, VoteLineBlock>>();

        public HashSet<Post> FutureReferences { get; } = new HashSet<Post>();

        #endregion

        #region Basic reset & tally
        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        public void Reset()
        {
            VoteBlockSupporters.Clear();

            ReferencePlanNames.Clear();
            ReferenceVoterNames.Clear();
            ReferencePlansEx3.Clear();

            PlanMessageIdEx3.Clear();
            VoterReferenceMessageIdEx3.Clear();
            VoterMessageIdEx3.Clear();

            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            RankedVotesWithSupporters.Clear();
            RankedVoterMessageId.Clear();
            PlanNames.Clear();

            FutureReferences.Clear();

            UndoBuffer.Clear();

            VoteDefinedTasks.Clear();
            OrderedVoteTaskList.Clear();

            taskList.Clear();

            cleanVoteLookup.Clear();
            cleanedKeys.Clear();

            if (VotesWithSupporters.Comparer != Agnostic.StringComparer)
                VotesWithSupporters = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);
            if (RankedVotesWithSupporters.Comparer != Agnostic.StringComparer)
                RankedVotesWithSupporters = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);
            if (ReferencePlansEx3.Comparer != Agnostic.StringComparer)
                ReferencePlansEx3 = new Dictionary<string, VoteLineBlock>(Agnostic.StringComparer);
            if (ReferencePlanNames.Comparer != Agnostic.StringComparer)
                ReferencePlanNames = new HashSet<string>(Agnostic.StringComparer);
            if (PlanNames.Comparer != Agnostic.StringComparer)
                PlanNames = new HashSet<string>(Agnostic.StringComparer);

            if (PlanMessageIdEx3.Comparer != Agnostic.StringComparer)
                PlanMessageIdEx3 = new Dictionary<string, int>(Agnostic.StringComparer);

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
            userMerges.Reset();
        }

        #endregion

        #region Handling Posts
        /// <summary>
        /// The list of posts collected from the quest. Read-only.
        /// </summary>
        public IReadOnlyList<Post> Posts => postsList;

        /// <summary>
        /// Add a new set of posts for the <see cref="IVoteCounter"/> to use.
        /// </summary>
        /// <param name="posts">The posts to be stored in the <see cref="IVoteCounter"/>.</param>
        public void AddPosts(IEnumerable<Post> posts)
        {
            postsList.Clear();
            if (posts != null)
                postsList.AddRange(posts);
        }

        /// <summary>
        /// Request that the currently stored posts be cleared.
        /// </summary>
        public void ClearPosts()
        {
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
        /// <param name="planBlock">The the vote line block that defines the plan.</param>
        /// <returns>Returns true if it was added, or false if it already exists.</returns>
        public bool AddReferencePlan(string planName, int postID, VoteLineBlock planBlock)
        {
            if (!ReferencePlanNames.Contains(planName, Agnostic.StringComparer))
            {
                ReferencePlanNames.Add(planName);
                PlanMessageIdEx3[planName] = postID;
                ReferencePlansEx3[planName] = planBlock;

                return true;
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
        public bool AddReferenceVoter(string voterName, int postID)
        {
            bool added = ReferenceVoterNames.Add(voterName);
            VoterReferenceMessageIdEx3[voterName] = postID;

            return added;
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

        #region Get Reference Data
        /// <summary>
        /// Get canonical version of the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan being checked for.</param>
        /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
        public string? GetProperPlanName(string planName)
        {
            return ReferencePlanNames.FirstOrDefault(n => Agnostic.InsensitiveComparer.Equals(n, planName));
        }

        /// <summary>
        /// Get canonical version of the provided voter name.
        /// </summary>
        /// <param name="voterName">The name of the voter being checked for.</param>
        /// <returns>Returns the reference version of the requested name, or null if not found.</returns>
        public string? GetProperVoterName(string voterName)
        {
            return ReferenceVoterNames.FirstOrDefault(v => Agnostic.StringComparer.Equals(v, voterName));
        }

        /// <summary>
        /// Get the post ID stored for the specified plan, which is the post that it was defined in.
        /// </summary>
        /// <param name="planName">The name of the plan to check on.</param>
        /// <returns>Returns the post ID for where the plan was defined, or null if not found.</returns>
        public int GetPlanReferencePostId(string planName)
        {
            return PlanMessageIdEx3.GetValueOrDefault(planName);
        }

        /// <summary>
        /// Get the post ID stored for the specified voter.  This will always be the last one entered.
        /// </summary>
        /// <param name="voterName">The name of the voter to check on.</param>
        /// <returns>Returns the post ID for the voter, or null if not found.</returns>
        public int GetVoterReferencePostId(string voterName)
        {
            return VoterReferenceMessageIdEx3.GetValueOrDefault(voterName);
        }

        /// <summary>
        /// Get the ID of the post by the specified author at the time of the request.
        /// This may change over the course of a tally.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns the post ID if the voter's most recently processed post, or 0 if not found.</returns>
        public int GetLatestVoterPostId(string voterName)
        {
            return VoterMessageIdEx3.GetValueOrDefault(voterName);
        }

        /// <summary>
        /// Get the last post made by a given author.
        /// Possibly restrict the search range to no more than the specified post ID.
        /// </summary>
        /// <param name="voterName">The voter being queried.</param>
        /// <param name="maxPostId">The highest post ID allowed. 0 means unrestricted.</param>
        /// <returns>Returns the last post by the requested author, if found. Otherwise null.</returns>
        public Post? GetLastPostByAuthor(string author, int maxPostId = 0)
        {
            if (!HasVoter(author))
                return null;

            var lastAuthorPost = postsList.Where(p =>
                                                Agnostic.StringComparer.Equals(author, p.Author)
                                                && (maxPostId == 0 || p.IDValue < maxPostId))
                                          .MaxObject(p => p.IDValue);

            return lastAuthorPost;
        }

        /// <summary>
        /// Get the reference plan corresponding to the provided plan name.
        /// </summary>
        /// <param name="planName">The name of the plan to get.</param>
        /// <returns>Returns the reference plan, if found. Otherwise null.</returns>
        public VoteLineBlock? GetReferencePlan(string planName)
        {
            return ReferencePlansEx3.GetValueOrDefault(planName);
        }

        /// <summary>
        /// Get a list of all vote blocks supported by a specified voter (which may be a plan name).
        /// </summary>
        /// <param name="voterName">The name of the voter or plan being requested.</param>
        /// <returns>Returns a list of all vote blocks supported by the specified voter or plan.</returns>
        public List<VoteLineBlock> GetVotesBy(string voterName)
        {
            return VoteBlockSupporters
                .SelectMany(a => a.Value)
                .Where(a => Agnostic.StringComparer.Equals(a.Key, voterName))
                .Select(a => a.Value)
                .ToList();
        }

        public int GetTotalVoterCount()
        {
            return ReferenceVoterNames.Count;
        }

        public IEnumerable<VoteLineBlock> GetSupportedVotesList()
        {
            foreach (var (vote, supporters) in VoteBlockSupporters)
            {
                vote.Category = GetCategoryOf(supporters);
                yield return vote;
            }
        }

        private MarkerType GetCategoryOf(Dictionary<string, VoteLineBlock> supporters)
        {
            int total = supporters.Count;

            if (total == 0)
                return MarkerType.None;

            double threshold = 0.83;

            var supporterMarkers = supporters.GroupBy(s => s.Value.MarkerType);

            foreach (var supporterMarker in supporterMarkers)
            {
                if (((double)supporterMarker.Count() / total) > threshold)
                {
                    return supporterMarker.Key;
                }
            }

            return MarkerType.Vote;
        }

        public IEnumerable<string> GetFullVotersList()
        {
            // TODO: Possibly filter out plans?
            var voters = VoteBlockSupporters.SelectMany(a => a.Value.Keys).Distinct().OrderBy(a => a);
            return voters;
        }

        public IEnumerable<string> GetVotersFor(VoteLineBlock vote)
        {
            if (VoteBlockSupporters.TryGetValue(vote, out var supporters))
            {
                return supporters.Select(a => a.Key);
            }

            return Enumerable.Empty<string>();
        }


        #endregion

        #region Query if counter Has ...
        /// <summary>
        /// Determine if the requested plan name exists in the current list of plans.
        /// Applies the plan name marker character to the provided plan name, if it
        /// doesn't already exist.
        /// </summary>
        /// <param name="planName">The name of the plan to check for.</param>
        /// <returns>Returns whether the provided plan name exists in the current PlanNames hash set.</returns>
        public bool HasPlan(string? planName)
        {
            if (string.IsNullOrEmpty(planName))
                return false;

            return ReferencePlanNames.Contains(planName, Agnostic.StringComparer);
        }

        /// <summary>
        /// Determines if the specified voter is in the list of all possible voters for the tally.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for.</param>
        /// <returns>Returns true if the voter has voted in the current tally.</returns>
        public bool HasVoter(string? voterName)
        {
            if (voterName is null)
                return false;

            return ReferenceVoterNames.Contains(voterName, Agnostic.StringComparer);
        }

        /// <summary>
        /// Determines whether the author of the provided post has made a newer vote submission.
        /// </summary>
        /// <param name="post">The post being checked.</param>
        /// <returns>Returns true if the voter has a newer vote already submitted.</returns>
        public bool HasNewerVote(Post post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (!HasVoter(post.Author))
                return false;

            var processedPosts = Posts.Where(p => p.Processed && p.Author == post.Author && p.IDValue > post.IDValue);

            return (processedPosts.Any());
        }
        #endregion

        #region Adding and Modifying Votes

        /// <summary>
        /// Add a collection of votes to the vote counter.
        /// </summary>
        /// <param name="votePartitions">A string list of all the parts of the vote to be added.</param>
        /// <param name="voter">The voter for this vote.</param>
        /// <param name="postID">The post ID for this vote.</param>
        /// <param name="voteType">The type of vote being added.</param>
        public void AddVotes(IEnumerable<VoteLineBlock> votePartitions, string voter, int postID)
        {
            if (!votePartitions.Any())
                return;

            // Store/update the post ID of the voter
            VoterMessageIdEx3[voter] = postID;

            // Remove the voter from any existing votes
            if (RemoveSupport(voter))
                OnPropertyChanged("Voters");

            // Add/update all segments of the provided vote
            foreach (var partition in votePartitions)
            {
                AddVote(partition, voter);
            }

            // Cleanup any votes that no longer have any support
            if (CleanupEmptyVotes())
                OnPropertyChanged("Votes");

        }

        /// <summary>
        /// Adds an individual vote.
        /// </summary>
        /// <param name="vote">The vote that is being added to.</param>
        /// <param name="voter">The voter that is supporting the vote.</param>
        /// <param name="voteType">Type of the vote.</param>
        /// <exception cref="System.ArgumentNullException">vote and voter must not be null or empty.</exception>
        private void AddVote(VoteLineBlock vote, string voter)
        {
            if (!VoteBlockSupporters.TryGetValue(vote, out var supporters))
            {
                var referenceVote = vote.WithMarker("", MarkerType.None, 0, allLines: true);
                supporters = new Dictionary<string, VoteLineBlock>(StringComparer.OrdinalIgnoreCase);

                VoteBlockSupporters.Add(referenceVote, supporters);
            }

            supporters.Add(voter, vote);

            AddPotentialVoteTask(vote.Task);

            OnPropertyChanged("Votes");
        }

        /// <summary>
        /// Merge the vote supporters from one vote into another.
        /// </summary>
        /// <param name="fromVote">The originating vote.</param>
        /// <param name="toVote">The destination vote.</param>
        /// <returns>Returns true if successfully completed.</returns>
        public bool Merge(VoteLineBlock fromVote, VoteLineBlock toVote)
        {
            UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteBlockSupporters));

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
        /// Merge the vote supporters from one vote into several other votes.
        /// </summary>
        /// <param name="fromVote">The originating vote.</param>
        /// <param name="toVotes">The destination votes.</param>
        /// <returns>Returns true if successfully completed.</returns>
        public bool Split(VoteLineBlock fromVote, List<VoteLineBlock> toVotes)
        {
            bool merged = false;

            UndoBuffer.Push(new UndoAction(UndoActionType.Split, VoteBlockSupporters));

            foreach (var toVote in toVotes)
            {
                merged = MergeImplWrapper(fromVote, toVote) || merged;
            }

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
        /// Shift support by various voters from their original vote to any votes
        /// supported by a specified target voter.
        /// </summary>
        /// <param name="voters">The voters that will support the new voter.</param>
        /// <param name="voterToJoin">The voter to join.</param>
        /// <returns>Returns true if successfully completed.</returns>
        public bool Join(List<string> voters, string voterToJoin)
        {
            bool joined = false;

            UndoBuffer.Push(new UndoAction(UndoActionType.Join, VoteBlockSupporters));

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
        }

        /// <summary>
        /// Delete an entire vote and all associated supporters.
        /// </summary>
        /// <param name="vote">The vote to delete.</param>
        /// <returns>Returns true if successfully completed.</returns>
        public bool Delete(VoteLineBlock vote)
        {
            bool removed = false;

            if (VoteBlockSupporters.ContainsKey(vote))
            {
                UndoBuffer.Push(new UndoAction(UndoActionType.Delete, VoteBlockSupporters));

                removed = VoteBlockSupporters.Remove(vote);
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

            if (!VoteBlockSupporters.TryGetValue(fromVote, out var fromSupport))
            {
                return false;
            }

            if (!VoteBlockSupporters.TryGetValue(toVote, out var toSupport))
            {
                return false;
            }

            // Theoretically, all the supporters in the from vote could already
            // be in the to vote, in which case no merging would happen.
            MergeImpl(fromVote, toVote, fromSupport, toSupport);

            // But we still want to remove the from vote.
            return VoteBlockSupporters.Remove(fromVote);
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
            Dictionary<string, VoteLineBlock> fromSupport, Dictionary<string, VoteLineBlock> toSupport)
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
        /// Implement joining logic without sending notifications.
        /// </summary>
        /// <param name="joiningVoter">The voter being moved to a new voting support set.</param>
        /// <param name="voterToJoin">The voter being joined.</param>
        /// <returns>Returns true if the join was completed.</returns>
        private bool JoinImpl(string joiningVoter, string voterToJoin)
        {
            var sourceVoterVotes = VoteBlockSupporters.Where(v => v.Value.Keys.Contains(joiningVoter)).ToList();

            var targetVoterVotes = VoteBlockSupporters.Where(v => v.Value.Keys.Contains(voterToJoin)).Select(v => v.Value).ToList();

            if (!sourceVoterVotes.Any() || !targetVoterVotes.Any())
                return false;

            bool joined = false;

            foreach (var (vote, support) in sourceVoterVotes)
            {
                // Don't remove support if you're already supporting the same ticket.
                if (!support.ContainsKey(voterToJoin))
                {
                    support.Remove(joiningVoter);
                    if (!support.Any())
                    {
                        VoteBlockSupporters.Remove(vote);
                        joined = true;
                    }
                }
            }

            foreach (var voteBlock in targetVoterVotes)
            {
                // Don't add if voter is already present
                if (!voteBlock.ContainsKey(joiningVoter))
                {
                    voteBlock.Add(joiningVoter, voteBlock[voterToJoin]);
                    joined = true;
                }
            }

            return joined;
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <returns>Returns true if any instances were removed.</returns>
        private bool RemoveSupport(string voter)
        {
            bool removedAny = false;

            foreach (var vote in VoteBlockSupporters)
            {
                if (vote.Value.Remove(voter))
                    removedAny = true;
            }

            return removedAny;
        }

        /// <summary>
        /// Removes any votes that no longer have any voter support.
        /// </summary>
        /// <returns>Returns true if any votes were removed.</returns>
        private bool CleanupEmptyVotes()
        {
            bool removedAny = false;

            // Any votes that no longer have any support can be removed
            var empty = VoteBlockSupporters.Where(v => v.Value.Count == 0).ToList();

            foreach (var vote in empty)
            {
                if (VoteBlockSupporters.Remove(vote.Key))
                    removedAny = true;
            }

            return removedAny;
        }

        #endregion


        #region Modifying Votes

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

            if (undo.Undo(this))
            {
                OnPropertyChanged("Votes");
                OnPropertyChanged("Voters");
                OnPropertyChanged(nameof(HasUndoActions));
                return true;
            }

            return false;
        }

        #endregion

        #region Private support methods
        /// <summary>
        /// Attempt to find any existing vote that matches with the vote we have,
        /// and can be used as a key in the VotesWithSupporters table.
        /// </summary>
        /// <param name="vote">The vote to search for.</param>
        /// <returns>Returns the string that can be used as a key in the VotesWithSupporters table.</returns>
        private string GetVoteKey(string vote, VoteType voteType)
        {
            if (Quest is null)
                throw new InvalidOperationException("Quest is null.");

            // Store a lookup of the cleaned version of the vote so we don't have to repeat the processing.
            if (!cleanedKeys.TryGetValue(vote, out string cleaned))
            {
                cleaned = VoteString.RemoveBBCode(vote);
                cleaned = VoteString.DeUrlContent(cleaned);
                cleanedKeys[vote] = cleaned;
            }

            var votes = GetVotesCollection(voteType);

            string returnVote = vote;

            // If the vote already matches an existing key, we don't need to search again.
            if (!votes.ContainsKey(vote))
            {
                // Find any vote that matches using an agnostic string comparison, that ignores
                // case, spacing, and most punctuation.
                string? agVote = votes.Keys.FirstOrDefault(k =>
                    Agnostic.StringComparer.Equals(cleaned, cleanedKeys[k]));

                // If we found a match, return that; otherwise this is a new vote, so return it unchanged.
                if (!string.IsNullOrEmpty(agVote))
                    returnVote = agVote;
            }

            if (userMerges.TryGetMergeRecord(returnVote, Quest.PartitionMode, out string mergedVoteKey))
            {
                if (!cleanedKeys.TryGetValue(mergedVoteKey, out string cleanMerge))
                {
                    cleanMerge = VoteString.RemoveBBCode(vote);
                    cleanMerge = VoteString.DeUrlContent(cleanMerge);
                    cleanedKeys[mergedVoteKey] = cleanMerge;
                }

                return mergedVoteKey;
            }

            return returnVote;
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="voteType">Type of the vote.</param>
        private bool RemoveSupport(string voter, VoteType voteType)
        {
            bool removedAny = false;

            if (voteType != VoteType.Plan)
            {
                var votes = GetVotesCollection(voteType);

                // Remove the voter from any existing votes
                foreach (var vote in votes)
                {
                    if (vote.Value.Remove(voter))
                        removedAny = true;
                }
            }

            return removedAny;
        }

        /// <summary>
        /// Removes any votes that no longer have any voter support.
        /// </summary>
        /// <param name="voteType">Type of the vote.</param>
        private bool CleanupEmptyVotes(VoteType voteType)
        {
            bool removedAny = false;

            var votes = GetVotesCollection(voteType);

            // Any votes that no longer have any support can be removed
            var emptyVotes = votes.Where(v => v.Value.Count == 0).ToList();
            foreach (var emptyVote in emptyVotes)
            {
                if (votes.Remove(emptyVote.Key))
                    removedAny = true;
            }

            return removedAny;
        }

        /// <summary>
        /// Will remove the specified voter from the voter ID list if there are no
        /// votes that they are currently supporting.
        /// </summary>
        /// <param name="voter">The voter to trim.</param>
        /// <param name="voteType">The type of vote to check.</param>
        private void TrimVoter(string voter, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);
            var voters = GetVotersCollection(voteType);

            if (!votes.Values.Any(v => v.Contains(voter)))
            {
                voters.Remove(voter);
            }
        }

        #endregion

        #region Task properties
        HashSet<string> VoteDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> UserDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> OrderedVoteTaskList { get; } = new List<string>();
        List<string> OrderedUserTaskList { get; } = new List<string>();
        public IReadOnlyList<string> TaskList => taskList;

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
        }

        /// <summary>
        /// Increases the task position in the task list.
        /// </summary>
        /// <param name="currentPosition">The task position to modify.</param>
        public void IncreaseTaskPosition(int currentPosition)
        {
            // The Swap extension function handles bounds checking.
            TaskList.Swap(currentPosition, currentPosition + 1);
            OnPropertyChanged("Tasks");
        }

        /// <summary>
        /// Decreases the task position in the task list.
        /// </summary>
        /// <param name="currentPosition">The task position to modify.</param>
        public void DecreaseTaskPosition(int currentPosition)
        {
            // The Swap extension function handles bounds checking.
            TaskList.Swap(currentPosition, currentPosition - 1);
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
                TaskList.AddRange(OrderedVoteTaskList);
                TaskList.AddRange(OrderedUserTaskList);
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

            if (VoteBlockSupporters.TryGetValue(vote, out var supporters))
            {
                UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteBlockSupporters, vote));

                VoteBlockSupporters.Remove(vote);

                string originalTask = vote.Task;
                vote.Task = task;

                // If there's a conflict with the newly-tasked vote, we need to merge with the existing vote.
                if (VoteBlockSupporters.ContainsKey(vote))
                {
                    if (VoteBlockSupporters.TryGetValue(vote, out var toSupport))
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

                        VoteBlockSupporters.Add(vote, supporters);

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

                    VoteBlockSupporters.Add(vote, supporters);
                }

                OnPropertyChanged("Votes");
                OnPropertyChanged(nameof(HasUndoActions));
                return true;
            }

            return false;
        }
        #endregion

        #region Implement INotifyPropertyChanged interface
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Deprecated

        #region Query on collection stuff

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;


        /// <summary>
        /// Get the dictionary collection of votes for the requested vote type.
        /// </summary>
        /// <param name="voteType">The type of vote being requested.</param>
        /// <returns>Returns a dictionary collection of the requested vote type.</returns>
        public Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVotesWithSupporters;
            else
                return VotesWithSupporters;
        }

        /// <summary>
        /// Get the dictionary collection of voters and post IDs for the requested vote type.
        /// </summary>
        /// <param name="voteType">The type of vote being requested.</param>
        /// <returns>Returns a dictionary collection of the requested voter type.</returns>
        public Dictionary<string, string> GetVotersCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVoterMessageId;
            else
                return VoterMessageId;
        }

        private HashSet<string> GetVoters(string vote, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            if (votes.ContainsKey(vote))
                return votes[vote];

            return new HashSet<string>(Agnostic.StringComparer);
        }

        /// <summary>
        /// Find all votes tied to a given vote line.
        /// The "plan name" (possibly user name) is checked with the
        /// standard and alternate extractions (adding a special marker character
        /// depending on whether the word "plan" is used, and whether it's 
        /// standard or alt) in order to look up votes that said (possible) voter
        /// supports.
        /// </summary>
        /// <param name="voteLine">The vote line to be checked.</param>
        /// <param name="author">The author of the vote. Prevent circular references.</param>
        /// <returns>Returns a list of all votes supported by the user or plan
        /// specified in the vote line, if found.  Otherwise returns an
        /// empty list.</returns>
        public List<string> GetVotesFromReference(string voteLine, string author)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));
            if (author == null)
                throw new ArgumentNullException(nameof(author));
            if (Quest is null)
                throw new InvalidOperationException("Quest is null.");

            List<string> results = new List<string>();

            var referenceNames = VoteString.GetVoteReferenceNames(voteLine);

            if (!referenceNames[ReferenceType.Any].Any())
                return results;

            string? proxyName = null;


            // Label ^ or '↑' means it must be a user reference.
            // Label 'plan' means it might be user or plan reference.

            if (referenceNames[ReferenceType.Label].Contains("plan"))
            {
                // If it's labeled as a 'plan', check plans first, then users.

                // If the reference exists as a plan, use it.
                if (referenceNames[ReferenceType.Plan].Any() && HasPlan(referenceNames[ReferenceType.Plan].First()))
                {
                    // Get the plan name from the recorded plan names collection.
                    proxyName = PlanNames.First(p => referenceNames[ReferenceType.Plan]
                        .Contains(VoteString.DeUrlContent(VoteString.RemoveBBCode(p)), Agnostic.StringComparer));
                }
                // If it doesn't exist as a plan, then we can check for users, as long as the quest hasn't disabled proxy votes.
                else if (!Quest.DisableProxyVotes &&
                        ReferenceVoterNames.Contains(referenceNames[ReferenceType.Voter].First(), Agnostic.InsensitiveComparer))
                {
                    proxyName = ReferenceVoterNames.First(n => referenceNames[ReferenceType.Voter].Contains(n, Agnostic.InsensitiveComparer));

                    // But don't allow self-references.  Some user names conflict with normal vote lines.
                    if (proxyName == author)
                        proxyName = null;
                }
            }
            else
            {
                // If it's not labeled as a 'plan', then it's either a pin or an unlabeled reference.
                // Either way, check for users first.

                // See if any voter names match the line (as long as we're allowing proxy votes).  If so, use that.
                if (!Quest.DisableProxyVotes &&
                    ReferenceVoterNames.Contains(referenceNames[ReferenceType.Voter].First(), Agnostic.InsensitiveComparer))
                {
                    proxyName = ReferenceVoterNames.First(n => referenceNames[ReferenceType.Voter].Contains(n, Agnostic.InsensitiveComparer));

                    // And make sure it's not the user using their own name.  Some user names conflict with normal vote lines.
                    if (proxyName == author)
                        proxyName = null;
                }
                // If a user isn't found, only check for regular plans if there's not a non-plan label being used,
                // but not if the quest requires plan vote lines to be labeled.
                else if (!Quest.ForcePlanReferencesToBeLabeled && !referenceNames[ReferenceType.Label].Any())
                {
                    if (referenceNames[ReferenceType.Plan].Any() && HasPlan(referenceNames[ReferenceType.Plan].First()))
                    {
                        // Get the plan name from the recorded plan names collection.
                        proxyName = PlanNames.First(p => referenceNames[ReferenceType.Plan]
                            .Contains(VoteString.DeUrlContent(VoteString.RemoveBBCode(p)), Agnostic.StringComparer));
                    }
                }
            }

            if (!string.IsNullOrEmpty(proxyName))
            {
                var planVotes = VotesWithSupporters.Where(v => v.Value.Contains(proxyName));

                if (planVotes.Count() > 1)
                    planVotes = VotesWithSupporters.Where(v => v.Value.Contains(proxyName, Agnostic.StringComparer));

                results.AddRange(planVotes.Select(v => v.Key));
            }

            return results;
        }
        #endregion


        #endregion

    }
}
