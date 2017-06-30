﻿using NetTally.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally.Votes.Experiment
{
    using PlanDictionary = Dictionary<string, List<Plan>>;

    /// <summary>
    /// Class that handles taking Posts as input, prepping the VotingRecords,
    /// extracting plans, and processing them all via the VotingConstructor.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class VotingCounter : INotifyPropertyChanged
    {
        #region Lazy singleton creation
        static readonly Lazy<VotingCounter> lazy = new Lazy<VotingCounter>(() => new VotingCounter());

        public static VotingCounter Instance => lazy.Value;

        VotingCounter()
        {
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Viewable Properties
        public IQuest CurrentQuest { get; private set; }
        public bool TallyWasCanceled { get; private set; }
        #endregion

        #region Watchable Properties
        bool voteCounterIsTallying;

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
        #endregion

        #region Public methods
        /// <summary>
        /// General function call to count the votes in the provided list of posts.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="posts">The posts to be processed.</param>
        /// <returns>Returns a Task, for async processing.</returns>
        public async Task CountVotesInPosts(IQuest quest, IEnumerable<Post> posts, CancellationToken token)
        {
            CurrentQuest = quest ?? throw new ArgumentNullException(nameof(quest));
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));

            VotingRecords.Instance.UsePostsForTally(posts);

            await CountVotesInCurrentPosts(token).ConfigureAwait(false);
        }

        /// <summary>
        /// General function call to count the votes in the currently stored list of posts.
        /// </summary>
        /// <returns>Returns a Task, for async processing.</returns>
        public async Task CountVotesInCurrentPosts(CancellationToken token)
        {
            if (CurrentQuest == null)
                return;

            if (!VotingRecords.Instance.PostsList.Any())
                return;

            try
            {
                VoteCounterIsTallying = true;
                TallyWasCanceled = false;

                VotingRecords.Instance.Reset();

                // Run sync functions as async, since they can take a while.
                await Task.Run(() => PreprocessPosts(token)).ConfigureAwait(false);
                await Task.Run(() => ProcessPosts(token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TallyWasCanceled = true;
            }
            finally
            {
                VoteCounterIsTallying = false;
            }
        }
        #endregion

        #region Private methods
        /// <summary>
        /// The first half of tallying posts involves doing the preprocessing
        /// work on the plans in the post list.
        /// </summary>
        private void PreprocessPosts(CancellationToken token)
        {
            // Collection of all the plans that we find in the post list.
            PlanDictionary planRepo = new PlanDictionary(Agnostic.StringComparer);

            // Scan the post list once.
            foreach (var post in VotingRecords.Instance.PostsList)
            {
                token.ThrowIfCancellationRequested();

                // Pull out all plans from each post.
                var plans = Plan.GetPlansFromVote(post.Vote);

                // Examine each plan.
                foreach (var plan in plans)
                {
                    if (VotingRecords.Instance.HasVoterName(plan.Identity.Name))
                    {
                        // A voter may name a plan after themselves.  No one else may.
                        // If they do, they're considered proxy votes, and we can skip to the next plan.
                        if (!Agnostic.StringComparer.Equals(plan.Identity.Name, post.Identity.Name))
                        {
                            continue;
                        }
                    }

                    if (planRepo.TryGetValue(plan.Identity.Name, out var existingPlans))
                    {
                        if (plan.PlanType < existingPlans[0].PlanType)
                        {
                            // Lower types are never added
                            continue;
                        }
                        else if (plan.PlanType > existingPlans[0].PlanType)
                        {
                            // A higher type wipes all existing plans, then adds itself as the new primary
                            existingPlans.Clear();
                            existingPlans.Add(plan);
                        }
                        else if (existingPlans.All(p => p != plan))
                        {
                            // If it's of the same tier, add it if it's a variant that's different from all existing plans.
                            plan.Identity.Number = existingPlans.Count;
                            existingPlans.Add(plan);
                        }
                    }
                    else
                    {
                        // If the plan name doesn't already exist, add it.
                        planRepo.Add(plan.Identity.Name, new List<Plan> { plan });
                    }
                }
            }

            // At the end, we should have a collection of the 'best' versions of each named plan.
            // Store them in the voting records.
            VotingRecords.Instance.AddPlans(planRepo);
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through all posts for
        /// as long as future references need to be handled.
        /// </summary>
        private void ProcessPosts(CancellationToken token)
        {
            // Process all the plans, to partition them and place them in the voting records.
            var plans = VotingRecords.Instance.GetPlans();

            foreach (var plan in plans)
            {
                VotingConstructor.ProcessPlan(plan, CurrentQuest, token);
            }

            // Set up each post with the working version that will be processed.
            VotingConstructor.ProcessPosts(CurrentQuest, token);
        }
        #endregion
    }
}
