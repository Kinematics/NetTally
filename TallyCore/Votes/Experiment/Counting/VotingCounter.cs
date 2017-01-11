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
    using PlanDictionary = Dictionary<string, (PlanType planType, string plan, string postId)>;

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
        public List<PostComponents> PostsList { get; private set; }
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
        public async Task CountVotesInPosts(IQuest quest, IEnumerable<PostComponents> posts, CancellationToken token)
        {
            CurrentQuest = quest ?? throw new ArgumentNullException(nameof(quest));
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));

            PostsList = new List<PostComponents>(posts);

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

            if (PostsList == null || PostsList.Count == 0)
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
            PlanDictionary planRepo = new PlanDictionary(Agnostic.StringComparer);

            // Scan the post list once.
            foreach (var post in PostsList)
            {
                token.ThrowIfCancellationRequested();

                // Keep a record of the most recent post ID for each user.
                VotingRecords.Instance.AddVoterName(post.Author);

                // Pull out all plans from each post.
                var plans = VotingConstructor.GetPlansFromPost(post, CurrentQuest);

                // Examine each plan.
                foreach (var plan in plans)
                {
                    // The plan must be named to be valid.
                    if (string.IsNullOrEmpty(plan.planName))
                        continue;

                    if (planRepo.TryGetValue(plan.planName, out var existingPlan))
                    {
                        // If the plan type found is 'higher' quality than any previously found plan types, replace the existing one.
                        if (plan.planType > existingPlan.planType)
                        {
                            planRepo[plan.planName] = (plan.planType, plan.plan, post.ID);
                        }
                    }
                    else
                    {
                        // If the plan name doesn't already exist, add it.
                        planRepo.Add(plan.planName, (plan.planType, plan.plan, post.ID));
                    }
                }
            }

            // At the end, we should have a collection of the 'best' versions of each named plan.
            // Store them in the voting records.
            VotingRecords.Instance.AddPlans(planRepo);

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in PostsList)
            {
                post.SetWorkingVote(p => VotingConstructor.GetWorkingVote(p));
            }
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through all posts for
        /// as long as future references need to be handled.
        /// </summary>
        private void ProcessPosts(CancellationToken token)
        {
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
