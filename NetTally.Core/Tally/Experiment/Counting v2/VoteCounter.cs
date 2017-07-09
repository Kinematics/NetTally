using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Utility;

namespace NetTally.Votes.Experiment2
{
    class VoteCounter : INotifyPropertyChanged
    {
        VoteRecords Records { get; } = new VoteRecords();

        #region Lazy singleton creation
        static readonly Lazy<VoteCounter> lazy = new Lazy<VoteCounter>(() => new VoteCounter());

        public static VoteCounter Instance => lazy.Value;

        VoteCounter()
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
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a Task, for async processing.</returns>
        public async Task CountVotesInPosts(IQuest quest, IEnumerable<Post> posts, CancellationToken token)
        {
            Records.Quest = quest ?? throw new ArgumentNullException(nameof(quest));
            Records.UsePosts(posts);

            await CountVotesInCurrentPosts(token).ConfigureAwait(false);
        }

        /// <summary>
        /// General function call to count the votes in the currently stored list of posts.
        /// </summary>
        /// <returns>Returns a Task, for async processing.</returns>
        public async Task CountVotesInCurrentPosts(CancellationToken token)
        {
            if (Records.Quest == null)
                return;

            if (!Records.Posts.Any())
                return;

            try
            {
                VoteCounterIsTallying = true;
                Records.Reset();

                // Run sync functions as async, since they can take a while.
                await Task.Run(() => PreprocessPosts(token)).ConfigureAwait(false);
                await Task.Run(() => ProcessPosts(token)).ConfigureAwait(false);
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
            Dictionary<string, List<RawVote>> planRepo = new Dictionary<string, List<RawVote>>(Agnostic.StringComparer);
            /*
            // Scan the post list once.
            foreach (var post in Records.Posts)
            {
                token.ThrowIfCancellationRequested();

                // Pull out all plans from each post.
                var plans = ;

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
            */
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through all posts for
        /// as long as future references need to be handled.
        /// </summary>
        private void ProcessPosts(CancellationToken token)
        {
            /*
            // Process all the plans, to partition them and place them in the voting records.
            var plans = VotingRecords.Instance.GetPlans();

            foreach (var plan in plans)
            {
                VotingConstructor.ProcessPlan(plan, CurrentQuest, token);
            }

            // Set up each post with the working version that will be processed.
            VotingConstructor.ProcessPosts(CurrentQuest, token);
            */
        }
        #endregion

    }
}
