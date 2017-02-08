using NetTally.Utility;
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
        private readonly List<Post> postsList = new List<Post>();
        public IReadOnlyList<Post> PostsList { get { return postsList; } }

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

            postsList.Clear();
            postsList.AddRange(posts.Where(p => p.HasVote));

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

            if (!PostsList.Any())
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
            // Record all the users who voted
            foreach (var post in PostsList)
            {
                VotingRecords.Instance.AddVoterRecord(post.Identity);
            }

            // Collection of all the plans that we find in the post list.
            PlanDictionary planRepo = new PlanDictionary(Agnostic.StringComparer);

            // Scan the post list once.
            foreach (var post in PostsList)
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
            // Set up each post with the working version that will be processed.
            foreach (var post in PostsList)
            {
                post.Prepare(p => VotingConstructor.GetWorkingVote(p));
            }

            var unprocessed = PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => VotingConstructor.ProcessPost(p, CurrentQuest, token) == true).ToList();

                // As long as some got processed, remove those from the unprocessed list
                // and let the loop run again.
                if (processed.Any())
                {
                    unprocessed = unprocessed.Except(processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var p in unprocessed)
                    {
                        p.ForceProcess = true;
                    }
                }
            }
        }
        #endregion
    }
}
